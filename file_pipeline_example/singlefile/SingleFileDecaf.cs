using System.Diagnostics;

namespace contoso.decaf.wrapper.singlefile;
/// <summary>Invokes 'logRfBin2Csv' for a single caf2 file.</summary>
internal class SingleFileDecaf
{
    const string _cafFileIndicator = "Input file = ";
    const string _cafIgnoreIndicator = "Input option, ignore";
    const string _cafSpecIndicator = "Caf2 Spec";
    const string _cafWaitingIndicator = "Press RETURN to finish";
    readonly static int _killTimeoutMs = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

    readonly SingleFileDecafConfig _config;

    /// <summary>Initialize with an <see cref="SingleFileDecafConfig"/> instance.</summary>
    public SingleFileDecaf(SingleFileDecafConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(paramName: nameof(config));
        _config = config;
    }

    /// <summary>
    /// Configure and start the logRfBin2Csv command line for one file.
    /// Capture stdout, stderr, and exit conditions and return them as an instance of <see cref="SingleFileDecafResult"/>.
    /// </summary>
    public Task<SingleFileDecafResult> RunAsync()
    {
        // wraps the process invocation as a task that can complete, fail, or be cancelled
        var processAsTask = new TaskCompletionSource<SingleFileDecafResult>();
        // buffers the stdout and stderr output of the process invocation
        var decafResult = new SingleFileDecafResult(_config);
        // the command line invocation of logRfBin2Csv
        Process decafProcess = null;

        // ------------------
        // local function that can access 'decafResult' with stdout
        // ------------------
        void DecafProcess_OutputDataReceived(object sender, DataReceivedEventArgs e) => CopyStdOutTo(decafResult, e, (sender as Process));

        // ------------------
        // local function that can access 'decafResult' with stderr
        // ------------------
        void DecafProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e) => CopyStdErrTo(decafResult, e);

        // ------------------
        // local function that can access 'taskResult' when process exits
        // ------------------
        void DecafProcess_Exited(object sender, EventArgs e)
        {
            DetachHandlers();
            DisposeProcess();
            processAsTask.TrySetResult(decafResult);
        }

        // ----------------
        // local function that can access 'decafProcess' for cleanup
        // ------------------
        void DetachHandlers()
        {
            if (decafProcess != null)
                try { decafProcess.OutputDataReceived -= DecafProcess_OutputDataReceived; } catch { }
            if (decafProcess != null)
                try { decafProcess.ErrorDataReceived -= DecafProcess_ErrorDataReceived; } catch { }
            if (decafProcess != null)
                try { decafProcess.Exited -= DecafProcess_Exited; } catch { }
        }

        void DisposeProcess() { try { decafProcess?.Dispose(); } catch { } }

        // ----------------
        // configure and start the process
        // capture stdout and stderr
        // listen for exited event
        // ------------------
        try
        {
            // validate config and set exception if not valid
            if (!_config.TryEnsureValid(out Exception validationEx))
            {
                processAsTask.TrySetException(validationEx);
                return processAsTask.Task;
            }

            decafProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Path.GetDirectoryName(_config.InputPathOfCaf2),
                    FileName = _config.PathToExecutable,
                    ArgumentList =
                        {
                            "--input",
                            _config.InputPathOfCaf2
                        }
                },
                EnableRaisingEvents = true
            };

            if (_config.IgnoreErrors)
                decafProcess.StartInfo.ArgumentList.Add("--ignore");

            decafProcess.EnableRaisingEvents = true;
            decafProcess.OutputDataReceived += DecafProcess_OutputDataReceived;
            decafProcess.ErrorDataReceived += DecafProcess_ErrorDataReceived;
            decafProcess.Exited += DecafProcess_Exited;

            // create a local cancellation token that will cancel after the allotted timeout
            // and register a cancellation callback
            _config.GetTimeoutToken().Register(DecafProcess_Cancelled);

            // start process
            decafProcess.Start();
            decafProcess.BeginOutputReadLine();
            decafProcess.BeginErrorReadLine();

            // ------------------
            // local function that can access 'decafProcess' and 'taskResult' when process is cancelled
            // ------------------
            void DecafProcess_Cancelled()
            {
                if (decafProcess != null)
                    try { decafProcess.CancelErrorRead(); } catch { }
                if (decafProcess != null)
                    try { decafProcess.CancelOutputRead(); } catch { }
                DetachHandlers();
                TryKillProcess(decafProcess);
                processAsTask.TrySetCanceled();
            }
        }
        catch (Exception processEx)
        {
            DetachHandlers();
            TryKillProcess(decafProcess);
            processAsTask.TrySetException(processEx);
        }

        // return task wrapper
        return processAsTask.Task;

    }

    void CopyStdOutTo(SingleFileDecafResult decafResult, DataReceivedEventArgs stdout, Process process)
    {
        if (stdout.Data != null)
        {
            // informational lines to ignore
            if (stdout.Data.StartsWith(_cafSpecIndicator))
                return;
            if (stdout.Data.StartsWith(_cafIgnoreIndicator))
                return;

            // trim 'Input file =' for consistency across versions
            if (stdout.Data.StartsWith(_cafFileIndicator))
            {
                decafResult.AppendOutput(stdout.Data.Replace(_cafFileIndicator, String.Empty));
            }
            else if (stdout.Data.StartsWith("Error") || stdout.Data.StartsWith("Fatal") || stdout.Data.Contains("fault") || stdout.Data.Contains("dump") || stdout.Data.Contains("terminate called"))
            {
                // in case those come from stdout rather than stderr
                decafResult.AppendError(stdout.Data);
            }
            else if (stdout.Data.StartsWith(_cafWaitingIndicator) == true)
            {
                if (process != null && process.StandardInput != null)
                {
                    // apparently either can be not null but disposed
                    try
                    {
                        // Press ENTER
                        process.StandardInput.WriteLine();
                        // looks like need to close stdin before process will exit
                        process.StandardInput.Close();
                    }
                    catch { }
                }
            }
        }
    }

    void CopyStdErrTo(SingleFileDecafResult decafResult, DataReceivedEventArgs stderr)
    {
        if (stderr.Data != null)
            decafResult.AppendError(stderr.Data);
    }

    void TryKillProcess(Process p)
    {
        try
        {
            // all three of these calls can throw exceptions even if the Handle is not null but there is no associated process;
            // however, that means there is no process to kill
            if (p?.HasExited == false)
            {
                p?.Kill();
                p?.WaitForExit(_killTimeoutMs);
            }
        }
        catch { }
        finally
        {
            p?.Dispose();
        }
    }

}
