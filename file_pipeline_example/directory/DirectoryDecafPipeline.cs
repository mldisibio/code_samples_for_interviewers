using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using contoso.logfiles.dataflow;
using contoso.decaf.wrapper.singlefile;

namespace contoso.decaf.wrapper.directory
{
    internal class DirectoryDecafPipeline
    {
        const string _caf2Filter = "*.caf2";

        public static ActionBlock<DirectoryDecafConfig> CreateSingleDirectoryPipeline(out BufferBlock<SingleFileDecafResult> tail, CancellationToken cancelToken = default)
        {
            var resultBuffer = new BufferBlock<SingleFileDecafResult>(new DataflowBlockOptions { CancellationToken = cancelToken });
            tail = resultBuffer;
            var discardNullDecafConfig = DataflowBlock.NullTarget<SingleFileDecafConfig>();
            var discardNullDecafResult = DataflowBlock.NullTarget<SingleFileDecafResult>();

            var reusableDirectoryPipe = new ActionBlock<DirectoryDecafConfig>(async dirConfig =>
            {
                try
                {
                    if (!dirConfig.TryEnsureValid(out Exception validityEx))
                        throw validityEx;

                    // --------------------
                    // note that the action block we are returning creates a new set of processing blocks each time a DirectoryDecafConfig is posted to it;
                    // this allows the subblocks to propagage completion, but the outer block can be reused until the caller submits completion signal;
                    // --------------------

                    // --------------------
                    // internally we will use the timeout cancellation token for our processing blocks
                    // this is a safety timeout for an entire directory
                    // we will use the caller's cancellation token to stop iterating individual files, allowing the last one to finish gracefully
                    // --------------------
                    CancellationToken timeoutToken = dirConfig.GetTimeoutToken();
                    var notIfBusy = new ExecutionDataflowBlockOptions { CancellationToken = timeoutToken, SingleProducerConstrained = true, BoundedCapacity = 1 };

                    // collection of caf2 files
                    string[] caf2Paths = GetCaf2Files(dirConfig) ?? Array.Empty<string>();
                    // requested parallelism or number of files, whichever is least
                    int pfx = FindMaxNeededConcurrency(caf2Paths, dirConfig.Pfx);
                    // collection to hold the parallel file processing streams
                    var processingTasks = new List<Task>(pfx);

                    // transform path to single file config
                    var pathToSingleFileConfigBlock = CreatePathToSingleFileConfigTransformBlock(dirConfig, notIfBusy);
                    // run <pfx> single file decafs in parallel; each process 'blocks' until it is finished, forcing a round robin
                    for (int i = 0; i < pfx; i++)
                    {
                        var singleFileDecafBlock = CreateSingleFileDecafProcessBlock(dirConfig, notIfBusy);
                        processingTasks.Add(singleFileDecafBlock.Completion);
                        // single file config is posted to a decaf process
                        pathToSingleFileConfigBlock.LinkToAndPropagateCompletion(singleFileDecafBlock, config => config != null);
                        pathToSingleFileConfigBlock.LinkTo(discardNullDecafConfig, config => config == null);

                        // decaf process yields a SingleFileDecafResult
                        singleFileDecafBlock.LinkTo(resultBuffer, result => result != null);
                        singleFileDecafBlock.LinkTo(discardNullDecafResult, result => result == null);
                    }

                    // since we will process each file only as pipelines are available
                    // cache the file list so that prolonged processing does not encounter a 'collection modified' event
                    Interlocked.Add(ref ProgressCounter.Instance.Caf2Found, caf2Paths.Length);

                    foreach (string caf2File in caf2Paths)
                    {
                        if (cancelToken.IsCancellationRequested || timeoutToken.IsCancellationRequested)
                            break;

                        await pathToSingleFileConfigBlock.SendAsync(caf2File).ConfigureAwait(false);
                    }
                    pathToSingleFileConfigBlock.Complete();
                    // the outer ActionBlock will 'block' until each parallel file process has completed
                    // but this does not mean the outer ActionBlock is complete yet - only when the caller signals it complete
                    await Task.WhenAll(processingTasks).ConfigureAwait(false);
                    // cleanup
                    TryRemoveRemainingZeroLengthFiles(dirConfig);
                    TryRemoveEmptyQuarantineFolder(dirConfig);
                }
                catch (Exception ex)
                {
                    ProgressCounter.Instance.AppendError($"SingleDirectoryPipeline: {ex.GetType().Name}: {ex.Message}");
                }

            }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1, CancellationToken = cancelToken, SingleProducerConstrained = true });

            // what we are saying here is that the outer ActionBlock can have more than one DirectoryDecafConfig posted to it by the caller;
            // (each time it will create a new set of parallel file processing blocks)
            // however, the outer ActionBlock has one result buffer to which it posts the SingleFileResults, so it cannot propagate completion 
            // to that buffer until the outer block is signaled complete;
            // when the caller signals the outer ActionBlock complete, that completion will be propagated to the result buffer,
            // and the caller can await completion of the result buffer
            reusableDirectoryPipe.Completion.ContinueWith(t => resultBuffer.Complete());

            return reusableDirectoryPipe;
        }

        static TransformBlock<string, SingleFileDecafConfig> CreatePathToSingleFileConfigTransformBlock(DirectoryDecafConfig directoryConfig, ExecutionDataflowBlockOptions execOpts)
        {
            return new TransformBlock<string, SingleFileDecafConfig>(caf2Path =>
            {
                DirectoryDecafConfig dirConfig = directoryConfig;
                try
                {
                    return new SingleFileDecafConfig()
                    {
                        InputPathOfCaf2 = caf2Path,
                        FinalOutputPathOfCsv = Path.Join(dirConfig.OutputDirectoryOfCsv, $"{Path.GetFileName(caf2Path)}.csv"),
                        PathToExecutable = dirConfig.PathToExecutable,
                        IgnoreErrors = dirConfig.IgnoreErrors,
                        TimeoutMs = dirConfig.SingleFileTimeoutMs
                    };
                }
                catch
                {
                    return null;
                }
            }, execOpts);
        }

        static TransformBlock<SingleFileDecafConfig, SingleFileDecafResult> CreateSingleFileDecafProcessBlock(DirectoryDecafConfig directoryConfig, ExecutionDataflowBlockOptions execOpts)
        {
            return new TransformBlock<SingleFileDecafConfig, SingleFileDecafResult>(async singleFileConfig =>
            {
                var handler = new TaskResultHandler
                {
                    FileConfig = singleFileConfig,
                    DirectoryConfig = directoryConfig,
                };
                var decaf = new SingleFileDecaf(singleFileConfig);
                return await decaf.RunAsync().ContinueWith<SingleFileDecafResult>((t, state) =>
                {
                    try
                    {
                        TaskResultHandler handler = state as TaskResultHandler;
                        // consume the exception so it is not bubbled as 'unhandled'
                        // optionally write to debug log
                        Exception acknowledgedException = null;
                        // for faulted or cancelled, we won't have a 'SingleFileResult'
                        if (t.IsFaulted)
                        {
                            acknowledgedException = t.Exception;
                            handler.TryQuarantineInvalidCaf2Source();
                            Interlocked.Increment(ref ProgressCounter.Instance.FailureCount);
                            return handler.CreateFaultedResult(acknowledgedException);
                        }
                        if (t.IsCanceled)
                        {
                            handler.TryQuarantineInvalidCaf2Source();
                            Interlocked.Increment(ref ProgressCounter.Instance.FailureCount);
                            return handler.CreateCancelledResult();
                        }
                        // for completed, the result might be successful or failed
                        if (t.IsCompleted && t.Result != null)
                        {
                            if (t.Result.DecompressedCsvCreated)
                            {
                                // non-zero csv output
                                handler.TryMoveDecafCsvToTarget(t.Result);
                                Interlocked.Increment(ref ProgressCounter.Instance.DecafCsv);
                                Interlocked.Add(ref ProgressCounter.Instance.OutputBytes, handler.ResultSize);
                            }
                            else
                            {
                                // completed but output is zero length
                                handler.TryRemoveFailedDecafOutput(t.Result);
                                handler.TryQuarantineInvalidCaf2Source();
                                Interlocked.Increment(ref ProgressCounter.Instance.FailureCount);
                            }
                            return t.Result;
                        }

                        // unexpected failsafe
                        ProgressCounter.Instance.AppendError("decaf.RunAsync().ContinueWith -> Unexpected Task Status");
                        return handler.CreateFaultedResult(new InvalidOperationException("Completed without result"));
                    }
                    catch(Exception ex)
                    {
                        ProgressCounter.Instance.AppendError($"decaf.RunAsync().ContinueWith -> UnhandledException: {ex.GetType().Name}: {ex.Message}");
                        Interlocked.Increment(ref ProgressCounter.Instance.FailureCount);
                        return null;
                    }

                }, state: handler).ConfigureAwait(false);


            }, execOpts);
        }

        static string[] GetCaf2Files(DirectoryDecafConfig config) => Directory.GetFiles(config.InputDirectoryOfCaf2, searchPattern: _caf2Filter, searchOption: SearchOption.TopDirectoryOnly);

        // will yield the lesser of actual file count or requestedConcurrency, without enumerating the entire directory
        static int FindMaxNeededConcurrency(IEnumerable<string> caf2Paths, short requestedConcurrency) => caf2Paths.Take(requestedConcurrency).Count();

        static void TryRemoveRemainingZeroLengthFiles(DirectoryDecafConfig config)
        {
            try
            {
                // use 'input directory' because the logRfBin2Csv executable can only decaf to the same directory as the input
                // and we are performing the cleanup before we move them to the configured output directory
                var srcDir = new DirectoryInfo(config.InputDirectoryOfCaf2);
                FileInfo[] csvFiles = srcDir.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
                foreach (FileInfo csvInfo in csvFiles)
                {
                    try
                    {
                        if (csvInfo.Length == 0)
                            csvInfo.Delete();
                    }
                    catch { }
                }
            }
            catch { }
        }

        static void TryRemoveEmptyQuarantineFolder(DirectoryDecafConfig config)
        {
            try
            {
                var srcDir = new DirectoryInfo(config.DirectoryForInvalidCaf2Files);
                if (srcDir.Exists)
                    if (!srcDir.EnumerateFiles().Any())
                        srcDir.Delete();
            }
            catch { }
        }

        private sealed class TaskResultHandler
        {
            internal SingleFileDecafConfig FileConfig { get; set; }
            internal DirectoryDecafConfig DirectoryConfig { get; set; }
            internal long ResultSize { get; set; }

            internal void TryMoveDecafCsvToTarget(SingleFileDecafResult result)
            {
                if (result?.ExpectedOutputCsvFilePath != null)
                {
                    //Console.WriteLine($"TryMove {result.ExpectedOutputCsvFilePath} to {FileConfig.FinalOutputPathOfCsv}");
                    try
                    {
                        MoveIfNotSame(result.ExpectedOutputCsvFilePath, FileConfig.FinalOutputPathOfCsv, overwrite: true);
                        result.FinalOutputCsvFilePath = FileConfig.FinalOutputPathOfCsv;
                        try { ResultSize = new FileInfo(FileConfig.FinalOutputPathOfCsv).Length; } catch { }
                    }
                    catch (Exception ex)
                    {
                        ProgressCounter.Instance.AppendError($"TryMove {result.ExpectedOutputCsvFilePath} to {FileConfig.FinalOutputPathOfCsv}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            internal void TryRemoveFailedDecafOutput(SingleFileDecafResult result)
            {
                if (result?.ExpectedOutputCsvFilePath != null)
                {
                    try { File.Delete(result.ExpectedOutputCsvFilePath); } catch { }
                }
            }

            internal void TryQuarantineInvalidCaf2Source()
            {
                if (FileConfig?.InputPathOfCaf2 != null)
                {
                    try
                    {
                        string src = FileConfig.InputPathOfCaf2;
                        string tgt = Path.Join(DirectoryConfig.DirectoryForInvalidCaf2Files, Path.GetFileName(FileConfig.InputPathOfCaf2));
                        MoveIfNotSame(src, tgt, overwrite: true);
                    }
                    catch { }
                }
            }

            internal SingleFileDecafResult CreateFaultedResult(Exception taskEx)
            {
                var result = new SingleFileDecafResult(FileConfig);
                result.SetExplicitFailure(taskEx == null ? "Decaf task exited with unhandled exception" : $"{taskEx.GetType().Name}: {taskEx.Message}");
                return result;
            }

            internal SingleFileDecafResult CreateCancelledResult()
            {
                var result = new SingleFileDecafResult(FileConfig);
                result.SetExplicitFailure("Decaf task was cancelled or timed out");
                return result;
            }

            /// <summary>Rename <paramref name="sourcePath"/> as <paramref name="destinationPath"/> if the two paths are not already the same.</summary>
            /// <param name="sourcePath">Full path of source file to be renamed.</param>
            /// <param name="destinationPath">Full path to <paramref name="sourcePath"/> will be renamed, if not already the same as <paramref name="sourcePath"/>.</param>
            /// <param name="overwrite">True to overwrite <paramref name="destinationPath"/>if it already exists.</param>
            static void MoveIfNotSame(string sourcePath, string destinationPath, bool overwrite)
            {
                FileInfo sourceFile = new FileInfo(sourcePath);
                if (sourceFile.Exists)
                {
                    string targetFile = new FileInfo(destinationPath).FullName;
                    if (!String.Equals(sourceFile.FullName, targetFile, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceFile.MoveTo(targetFile, overwrite: overwrite);
                    }
                }
            }
        }

    }
}
