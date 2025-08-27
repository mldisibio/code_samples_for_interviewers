using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using contoso.decaf.wrapper.directory;
using contoso.logfiles.dataflow;

namespace contoso.decaf.wrapper
{
    /// <summary>
    /// An <see cref="IPropagatorBlock{DirectoryDecafConfig, SingleFileDecafResult}"/> core data flow unit of work to find caf2 files
    /// from one directory, decompress them in parallel, and return a stream of zero, one, or more decompressed csv results.
    /// </summary>
    public class DirectoryDecafPath : PropagatorFlowPath<DirectoryDecafConfig, SingleFileDecafResult>
    {
        TransformBlock<DirectoryDecafConfig, DirectoryDecafConfig> _head;
        BufferBlock<SingleFileDecafResult> _tail;

        /// <summary>
        /// Initialize with a token that represents a general cancellation action.
        /// Internally, any configured timeouts will also cancel the processing.
        /// </summary>
        public DirectoryDecafPath(CancellationToken cancelToken = default)
            : base(cancelToken)
        {
            CreatePipeline();
        }

        /// <summary>Head will accept one input at a time for processing (allowing for caller to setup a round-robin parallelization for multiple directories).</summary>
        protected override ITargetBlock<DirectoryDecafConfig> CreateHead()
        {
            // this could easily be a simple BufferBlock, but on cold start does not return 'busy' to the posting block without a slight delay;
            // if trying to round-robin with a few directories, an entire parallel stream can be wasted because one accepts two posts before signaling 'busy'
            _head = new TransformBlock<DirectoryDecafConfig, DirectoryDecafConfig>(async cfg =>
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
            var directoryPipeline = DirectoryDecafPipeline.CreateSingleDirectoryPipeline(out BufferBlock<SingleFileDecafResult> resultBuffer, base.CancelToken);
            _head.LinkToAndPropagateCompletion(directoryPipeline);
            resultBuffer.LinkToAndPropagateCompletion(_tail);
        }

    }
}
