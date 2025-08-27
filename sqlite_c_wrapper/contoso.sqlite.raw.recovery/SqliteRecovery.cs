using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace contoso.sqlite.raw.recovery
{
    /// <summary>
    /// Attempt to repair a damaged sqlite file by invoking the sqlite shell, executing the '.dump' command,
    /// and creating a new sqlite file from the dump output.
    /// </summary>
    public class SqliteRecovery
    {
        const string _rollback = "ROLLBACK";
        const string _commit = "COMMIT";
        readonly static int _longTimeoutMs = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;
        readonly static int _shortTimeoutMs = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

        readonly SqliteRecoveryConfig _recoveryConfig;
        readonly StdErrPassThru _producerStdErrHandler;
        readonly StdErrPassThru _consumerStdErrHandler;

        readonly TaskCompletionSource<bool> _producerExitEventTask;
        readonly TaskCompletionSource<bool> _producerCompletedTask;
        readonly TaskCompletionSource<bool> _consumerCompletedTask;

        /// <summary>Initialize with process arguments wrapped by <see cref="SqliteRecoveryConfig"/>.</summary>
        public SqliteRecovery(SqliteRecoveryConfig config)
        {
            _recoveryConfig = config;
            // collects stream from the '.dump' command StdErr
            _producerStdErrHandler = new StdErrPassThru(3);
            // collects and discards stream from the consumer StdErr
            _consumerStdErrHandler = new StdErrPassThru(3);

            _producerExitEventTask = new TaskCompletionSource<bool>();
            _producerCompletedTask = new TaskCompletionSource<bool>();
            _consumerCompletedTask = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Error string, if any, from the consumer.
        /// In this case, errors from executing the .dump sql are expected but seem to be ignored by sqlite itself.
        /// </summary>
        public string? ErrorsFromConsumer => _consumerStdErrHandler?.ToString();

        /// <summary>
        /// Error string, if any, from the producer.
        /// In this case, if the command line '.dump' issues an error, it usually also exits the process.
        /// </summary>
        public string? ErrorsFromProducer => _producerStdErrHandler?.ToString();

        /// <summary>
        /// Attempt to repair a damaged sqlite file by invoking the sqlite shell, executing the '.dump' command,
        /// and creating a new sqlite file from the dump output.
        /// </summary>
        public async Task AttemptDumpToNewFile()
        {
            // validation of paths
            string pathToCmd = Path.GetFullPath(_recoveryConfig.PathToSqliteShell);
            string inputPath = Path.GetFullPath(_recoveryConfig.SqliteInputPath);
            string outputPath = Path.GetFullPath(_recoveryConfig.SqliteOutputPath);
            // ensure the output is not the same as the input
            if (outputPath.Equals(inputPath, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Cannot write output to same path as input");
            // ensure the output does not exist, otherwise sqlite will append into it
            File.Delete(outputPath);

            // allow all exceptions to be thrown back to caller
            // configure and start the consumer and get buffer that wraps writing to the consumer StandardInput
            using Process sqlConsumer = ConfigureConsumer(pathToCmd, outputPath);
            ActionBlock<string> consumerBuffer = StartConsumer(sqlConsumer);

            // configure and start producer with the buffer it will write to
            using Process sqlProducer = ConfigureProducer(pathToCmd, inputPath, consumerBuffer);
            StartProducer(sqlProducer, consumerBuffer);

            // wait for both producer and consumer to complete
            await Task.WhenAll(_producerCompletedTask.Task, _consumerCompletedTask.Task).ConfigureAwait(false);

            // throw any captured exceptions
            if (_consumerCompletedTask.Task.IsFaulted && _consumerCompletedTask.Task.Exception != null)
                throw _consumerCompletedTask.Task.Exception;
            if (_producerCompletedTask.Task.IsFaulted && _producerCompletedTask.Task.Exception != null)
                throw _producerCompletedTask.Task.Exception;
        }

        Process ConfigureConsumer(string pathToCmd, string pathToOutputFile)
        {
            // -----------
            // a local function that can be used as a named delegate for process events
            // -----------
            // any confirmations from executing the '.dump' output and sent to StdOut will be ignored
            void handleConsumerError(object s, DataReceivedEventArgs e)
            {
                if (!String.IsNullOrWhiteSpace(e.Data))
                    _consumerStdErrHandler.RecordErrorLine(e.Data);
            }

            var sqlConsumer = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    // any confirmations from executing the '.dump' output and sent to StdOut will be ignored
                    //RedirectStandardOutput = true,
                    FileName = pathToCmd,
                    Arguments = pathToOutputFile
                }
            };

            // if sql that is emitted from dump causes error on execution, it will be written to StdErr;
            // this is expected and these messages, although errors, can be ignored because the restore process itself ignores them
            sqlConsumer.ErrorDataReceived += handleConsumerError;

            return sqlConsumer;
        }

        ActionBlock<string> StartConsumer(Process sqlConsumer)
        {
            // start the listener which will copy the '.dump' output to a new database
            bool consumerStarted = sqlConsumer.Start();
            // read any errors from executing the '.dump' output, but these will be ignored
            sqlConsumer.BeginErrorReadLine();

            if (!consumerStarted)
                throw new InvalidOperationException("Sqlite consumer process did not start.");

            // with the stream now opened, create the buffer that will write to the consumer StandardInput
            // and to which the producer will post its output
            var writeToStandardInput = new ActionBlock<string>(line =>
            {
                if (line.StartsWith(_rollback))
                    // since the dump is from a database with errors, it will end with 'ROLLBACK', so replace with 'COMMIT'
                    sqlConsumer.StandardInput.WriteLine(line.Replace(_rollback, _commit));
                else
                    sqlConsumer.StandardInput.WriteLine(line);

            }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true });

            // start the consumer listening task; it will set the consumer's TaskCompletionResult when completed;
            var consumerListening = WaitForConsumer(sqlConsumer, writeToStandardInput.Completion);

            // return the delegate for the producer to use to write to the consumer StandardInput
            return writeToStandardInput;
        }

        async Task WaitForConsumer(Process sqlConsumer, Task inputCompleteTask)
        {
            // since we are writing to StandardInput, we are reponsible for closing it,
            // but only after it has finished processing its input
            using (sqlConsumer.StandardInput)
            {
                // wait, up to a timeout, for the input buffer to be emptied
                await Task.WhenAny(inputCompleteTask, Task.Delay(_longTimeoutMs)).ConfigureAwait(false);
            } // will close the listener StdIn and thus end the listener process

            // wait for the listener to exit gracefully, up to a timeout
            if (sqlConsumer.WaitForExit(_shortTimeoutMs))
            {
                // exit signal received before timeout
                // recommended to invoke this again without timeout
                sqlConsumer.WaitForExit();
                // set completion task result; this is happy path
                _consumerCompletedTask.TrySetResult(true);
            }
            else
            {
                // we've timed out; cancel any asynchronous operations
                try { sqlConsumer.CancelErrorRead(); } catch { }
                // attempt to force it to exit
                TryKillProcess(sqlConsumer);
                // and communicate that the consumer completed, but not gracefully
                _consumerCompletedTask.TrySetException(new TimeoutException("sqlite3 'read from STDIN' process timed out."));
            }
        }

        Process ConfigureProducer(string pathToCmd, string pathToInputFile, ActionBlock<string> consumerBuffer)
        {
            // -----------
            // set of three local functions that can be used as named delegates for process events
            // -----------
            // if '.dump' produces errors, then the file is too corrupt to continue
            // observation seems to indicate this will also cause the '.dump' process to exit
            void handleProducerError(object s, DataReceivedEventArgs e)
            {
                if (!String.IsNullOrWhiteSpace(e.Data))
                {
                    // record any errors
                    _producerStdErrHandler.RecordErrorLine(e.Data);
                }
            }

            // finish configuring with the delegate that writes to the consumer StandardInput
            // feed the output from '.dump' to the consumer's buffer
            void handleProducerOutput(object s, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                {
                    // NULL output indicates there is no more output to process,
                    // so signal the consumer buffer to stop waiting for input
                    consumerBuffer.Complete();
                }
                else
                {
                    // write the output line to the consumer input stream
                    consumerBuffer.Post(e.Data);
                }
            }

            // we'll use the Exited event to asynchronously wait for the process to exit
            // and we'll watch for it using a local TaskCompletionSource
            void handleProducerExited(object? s, EventArgs e) => _producerExitEventTask.TrySetResult(true);

            var sqlProducer = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = pathToCmd,
                    // the full command is 'sqlite3 pathToCorruptDb.dat .dump', so arguments are 'path .dump' NOT an output file 'pathToCorruptDb.dat.dump'; output is a stream, not a file;
                    Arguments = $"{pathToInputFile} .dump"
                }
            };

            sqlProducer.EnableRaisingEvents = true;
            // if '.dump' produces errors, then the file is too corrupt to continue
            sqlProducer.ErrorDataReceived += handleProducerError;
            // feed the output from '.dump' to the consumer's buffer
            sqlProducer.OutputDataReceived += handleProducerOutput;
            // set task completion when exited
            sqlProducer.Exited += handleProducerExited;

            return sqlProducer;
        }

        void StartProducer(Process sqlProducer, ActionBlock<string> consumerBuffer)
        {
            // start the writer which outputs sql from '.dump'
            bool producerStarted = sqlProducer.Start();

            if (!producerStarted)
            {
                consumerBuffer.Complete();
                throw new InvalidOperationException("Sqlite producer process did not start.");
            }

            // read any error from '.dump' itself; these are critical
            sqlProducer.BeginErrorReadLine();
            // read the sql output of '.dump' which will be written to the consuming process
            sqlProducer.BeginOutputReadLine();

            // start the producer writing task; it will set the producer's TaskCompletionResult when completed;
            Task _ = WaitForProducer(sqlProducer, consumerBuffer);
        }

        async Task WaitForProducer(Process sqlProducer, ActionBlock<string> consumerBuffer)
        {
            // wait for the writer to finish, up to a timeout
            await Task.WhenAny(_producerExitEventTask.Task, Task.Delay(_longTimeoutMs)).ConfigureAwait(false);

            // we've completed one way or another, so in all cases signal completion to consumer
            consumerBuffer.Complete();

            // determine if completion was graceful or not
            // if the internal signal is set complete, that means we did not timeout
            if (_producerExitEventTask.Task.IsCompletedSuccessfully)
            {
                // but we need to check if we exited early because of .dump errors
                if (_producerStdErrHandler.HasErrors)
                {
                    string errMsg = $"The '.dump' command generated errors: [{_producerStdErrHandler}]";
                    // communicate that the producer completed, but with errors
                    _producerCompletedTask.TrySetException(new InvalidOperationException(errMsg));
                }
                else
                {
                    // we appear to have exited gracefully
                    _producerCompletedTask.TrySetResult(true);
                }
            }
            else
            {
                // we've timed out; cancel any asynchronous operations
                try { sqlProducer.CancelErrorRead(); } catch { }
                try { sqlProducer.CancelOutputRead(); } catch { }
                // attempt to force it to exit
                TryKillProcess(sqlProducer);
                // and communicate that the consumer completed, but not gracefully
                _producerCompletedTask.TrySetException(new TimeoutException("sqlite3 '.dump to STDOUT' timed out."));
            }
        }

        static void TryKillProcess(Process p)
        {
            try
            {
                // all three of these calls can throw exceptions even if the Handle is not null but there is no associated process;
                // however, that means there is no process to kill
                if (p?.HasExited == false)
                {
                    p.Kill();
                    p.WaitForExit(_shortTimeoutMs);
                }
            }
            catch { }
        }
    }
}
