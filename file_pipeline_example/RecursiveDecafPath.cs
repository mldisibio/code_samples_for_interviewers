using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using contoso.decaf.wrapper.directory;
using contoso.logfiles.common;
using contoso.logfiles.dataflow;
using contoso.utility.fluentextensions;

namespace contoso.decaf.wrapper
{
    /// <summary>
    /// An <see cref="IPropagatorBlock{RecursiveDecafConfig, SingleFileDecafResult}"/> core data flow unit of work to recurse all caf2 files
    /// from a parent directory and its subdirectories, decompress them in parallel, and return a stream of zero, one, or more decompressed csv results.
    /// </summary>
    public class RecursiveDecafPath : PropagatorFlowPath<RecursiveDecafConfig, SingleFileDecafResult>
    {
        static readonly char[] _wildcards = new char[] { '*', '%' };
        TransformBlock<RecursiveDecafConfig, RecursiveDecafConfig> _head;
        BufferBlock<SingleFileDecafResult> _tail;

        /// <summary>
        /// Initialize with a token that represents a general cancellation action.
        /// Internally, any configured timeouts will also cancel the processing.
        /// </summary>
        public RecursiveDecafPath(CancellationToken cancelToken = default)
            : base(cancelToken)
        {
            CreatePipeline();
        }

        /// <summary>Head will accept one input at a time for processing (allowing for caller to setup a round-robin parallelization for multiple directories).</summary>
        protected override ITargetBlock<RecursiveDecafConfig> CreateHead()
        {
            // this could easily be a simple BufferBlock, but on cold start does not return 'busy' to the posting block without a slight delay;
            // if trying to round-robin with a few directories, an entire parallel stream can be wasted because one accepts two posts before signaling 'busy'
            _head = new TransformBlock<RecursiveDecafConfig, RecursiveDecafConfig>(async cfg =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                return cfg;
            }, ExecutionOptions.NotIfBusy);
            return _head;
        }

        /// <summary>
        /// Returns the the final processing block whose output of <see cref="SingleFileDecafResult"/> can be linked to another target block or consumed directly.
        /// </summary>
        protected override IReceivableSourceBlock<SingleFileDecafResult> CreateTail()
        {
            _tail = new BufferBlock<SingleFileDecafResult>(ExecutionOptions.SingleProducerWithCancel);
            return _tail;
        }

        /// <summary>Returns the Task that represents the asynchronous operation and completion of the dataflow block.</summary>
        /// <remarks>Caller must consume the output of this data flow block for it to complete.</remarks>
        public override Task Completion => Tail.Completion;

        void CreatePipeline()
        {
            // simple pass-thru buffer to which subdirectory requests will be posted to parallel decaf pipelines; Transform round-robins better than Buffer
            var decafInputBuffer = new TransformBlock<DirectoryDecafConfig, DirectoryDecafConfig>(input => input, ExecutionOptions.NotIfBusySingleProducer);
            // simple buffer to collect results from parallel directory pipelines and pass-thru to 'tail'
            var decafOutputBuffer = new BufferBlock<SingleFileDecafResult>(ExecutionOptions.WithCancel);
            // block to create and invoke as many parallel directory pipelines as can be equally distributed for allowed cores
            var recursePipeline = new ActionBlock<RecursiveDecafConfig>(async request =>
            {
                if (request != null)
                {
                    try
                    {
                        if (!request.TryEnsureValid(out Exception validityEx))
                            throw validityEx;

                        // cache all candidate directories as List to avoid 'collection modified' errors while enumerating over extended time
                        var directoryList = EnumerateCaf2DirectoriesStartingWith(request.StartingRootDirectory, startsWith: request.Caf2DirectoryStartsWith).ToList();
                        int parallelDirectories = FindMaxParallelDirectories(directoryList, totalCoresAllowed: request.Pfx, out int coresPerDirectory);
                        ProgressCounter.Instance.AppendInfo($"Parallel Dirs: {parallelDirectories}  Cores Per Dir: {coresPerDirectory}");

                        // create config for each subdirectory with caf2 files with the condition they have some serial number in the path
                        var directoryConfigs = directoryList.Select(subDirPath => ComposeSubDirectoryConfig(subDirPath, request, (short)coresPerDirectory))
                                                            .Where(subDirConfig => subDirConfig != null);

                        // manage each parallel directory pipe
                        var decafTaskList = new List<Task>(parallelDirectories);
                        // the parallel data pipes
                        for (int i = 0; i < parallelDirectories; i++)
                        {
                            int pipeId = i + 1;
                            DirectoryDecafPath decafPath = new DirectoryDecafPath(base.CancelToken);

                            decafInputBuffer.LinkToAndPropagateCompletion(decafPath);
                            decafPath.LinkTo(decafOutputBuffer); // multiple sources, so cannot propagate completion
                            decafTaskList.Add(decafPath.Completion.ContinueWith(_ => ProgressCounter.Instance.AppendInfo($"Directory Pipe [{pipeId}] completed")));
                        }
                        // post each config for each caf2 subdirectory to a decaf pipeline
                        foreach (var subdirConfig in directoryConfigs)
                        {
                            await decafInputBuffer.SendAsync(subdirConfig).ConfigureAwait(false);
                        }
                        decafInputBuffer.Complete();
                        
                        // await completion of all parallel decaf pipelines
                        await Task.WhenAll(decafTaskList).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ProgressCounter.Instance.AppendError($"Recurse Pipeline: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                // BoundedCapacity=1 only accepts one request until finished processing, forcing a round-robin if the caller creates multiple instances of this pipe
            }, ExecutionOptions.NotIfBusySingleProducer);

            // the caller posts to 'head', possible more than once; completion signaled by caller will propagate
            _head.LinkToAndPropagateCompletion(recursePipeline);
            // the output buffer will pass results thru to 'tail' but will not be signaled completed by decaf pipelines, but rather by 'head'
            decafOutputBuffer.LinkToAndPropagateCompletion(_tail);
            // when caller signals complete, completion is propagated; output buffer is completed, and in turn 'tail' will be signaled complete
            recursePipeline.Completion.ContinueWith(_ => decafOutputBuffer.Complete());
        }


        static IEnumerable<string> EnumerateCaf2DirectoriesStartingWith(string current, string startsWith)
        {
            string startsWithTrimmed = startsWith.IsNullOrEmptyString() ? null : startsWith.TrimEnd(_wildcards);
            // get all subdirectories, whether they match 'startsWith' or not
            var subDirectories = GetSubDirectories(current);
            // recurse
            foreach (string subDir in subDirectories)
                foreach (string match in EnumerateCaf2DirectoriesStartingWith(subDir, startsWithTrimmed))
                    yield return match;
            // if here, directory has no subdirectories, so see if it matches 'starts with' and has caf2
            if (HasCaf2Files(current, startsWithTrimmed))
                yield return current;

            IEnumerable<string> GetSubDirectories(string parentDir)
            {
                try { return Directory.EnumerateDirectories(parentDir); }
                catch { return Enumerable.Empty<string>(); }
            }

            // this directory has no subdirectories, so see if it matches 'starts with' and has caf2 files;
            // (assumption is 'startsWith' is an S/N prefix and caf2 always in an S/N directory)
            bool HasCaf2Files(string path, string startsWith)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(Path.GetFullPath(path));
                    bool isFilterMatch = startsWith == null || dirInfo.Name.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase);
                    return isFilterMatch && dirInfo.EnumerateFiles(searchPattern: "*.caf2", SearchOption.TopDirectoryOnly).Any();
                }
                catch { }
                return false;
            }
        }

        static int FindMaxParallelDirectories(IEnumerable<string> directorySequence, int totalCoresAllowed, out int coresPerDirectory)
        {
            // start with assumption of one directory at a time
            int actualParallelDirs = 1;
            coresPerDirectory = totalCoresAllowed;
            if (totalCoresAllowed > 3)
            {
                // lets say we can use up to 32 cores; instead of 32 directories at a time, each with one core,
                // we want at least two cores per directory stream (arbitrary)
                // so we start with assumption we can have up to 16 parallel directories
                int maxParallelDirs = totalCoresAllowed / 2;
                // now find if the actual count of directories is less that that
                actualParallelDirs = directorySequence.Take(maxParallelDirs).Count();
                // if we have less than the max allotted (allowing for two threads per directory) we can re-apportion more cores per directory
                coresPerDirectory = actualParallelDirs > 0 ? Math.Max(totalCoresAllowed / actualParallelDirs, 2) : 1;
            }
            return actualParallelDirs;
        }

        static DirectoryDecafConfig ComposeSubDirectoryConfig(string candidatePath, RecursiveDecafConfig rootConfig, short coresPerDirectory)
        {
            try
            {
                var snFilter = new SerialNumberFilter(candidatePath);
                if (!snFilter.Success)
                    throw new InvalidOperationException($"Serial Number required in path");

                string outputPrefix = PrefixComposer.ComposeDirectoryPrefix(snFilter.SerialNumber, rootConfig.OutputPrefix);
                var subDirConfig = new DirectoryDecafConfig
                {
                    InputDirectoryOfCaf2 = candidatePath,
                    OutputDirectoryOfCsv = outputPrefix == null
                                                           ? Path.Join(rootConfig.OutputDirectoryOfCsv, snFilter.SerialNumber)
                                                           : Path.Join(rootConfig.OutputDirectoryOfCsv, outputPrefix, snFilter.SerialNumber),
                    PathToExecutable = rootConfig.PathToExecutable,
                    IgnoreErrors = rootConfig.IgnoreErrors,
                    DirectoryTimeoutMs = rootConfig.TimeoutMs,
                    Pfx = coresPerDirectory
                };
                if (subDirConfig.TryEnsureValid(out Exception validityEx))
                    return subDirConfig;
                else
                    throw validityEx;
            }
            catch (Exception ex)
            {
                ProgressCounter.Instance.AppendError($"{ex.GetType().Name}: {ex.Message} [{candidatePath}]");
            }
            return null;
        }


    }
}
