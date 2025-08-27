using System.Threading.Tasks.Dataflow;

namespace contoso.utility.iohelp;

/// <summary>
/// Enables a stream of messages to be written to the underlying file
/// in batch mode, in the order received, without blocking the calling thread.
/// </summary>
public sealed class QueuedBatchFileWriter : IAsyncDisposable, IDisposable
{
    readonly List<Tuple<string[], string>> _errors = new List<Tuple<string[], string>>();
    readonly BatchBlock<string> _queue;
    readonly ActionBlock<string[]> _batchWriter;
    readonly StreamWriter _writer;
    bool _alreadyDisposed;

    QueuedBatchFileWriter(StreamWriter writer, int batchSize, CancellationToken cancelToken)
    {
        _writer = writer;
        var execOpts = new ExecutionDataflowBlockOptions { CancellationToken = cancelToken, SingleProducerConstrained = true };
        var withCompletionPropagation = new DataflowLinkOptions { PropagateCompletion = true };
        _queue = new BatchBlock<string>(batchSize);
        _batchWriter = new ActionBlock<string[]>(WriteBatch, execOpts);
        _queue.LinkTo(_batchWriter, withCompletionPropagation);
    }

    /// <summary>
    /// Returns an instance of <see cref="QueuedBatchFileWriter"/> which enables a stream of messages
    /// to be written to <paramref name="filePath"/> without blocking the current thread.
    /// <see cref="QueuedBatchFileWriter"/> implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>
    /// and must be disposed when finished to flush the queued messages to the file and to release underlying resources.
    /// </summary>
    public static QueuedBatchFileWriter CreateOver(string filePath, int batchSize, CancellationToken cancelToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(paramName: nameof(filePath));

        string fullpath = Path.GetFullPath(filePath).WithParentDirectoryCreation();
        var writer = new StreamWriter(fullpath);
        return new QueuedBatchFileWriter(writer, batchSize, cancelToken);
    }

    /// <summary>
    /// Returns the collection of errors, if any, with each item containing the line that failed and the exception message.
    /// This collection is available even after closing the writer if the instance has not been dereferenced.
    /// </summary>
    public IEnumerable<Tuple<string[], string>> ErrorMessages => _errors;

    /// <summary>True if one or more submitted strings were not written to disk and an error was encountered.</summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Enqueues <paramref name="line"/> to be written to the underlying file
    /// without blocking the current thread.
    /// </summary>
    /// <param name="line">
    /// The full line to write to the file. If null, an empty line will be written.
    /// </param>
    public void Enqueue(string line)
    {
        ThrowIfDisposed();
        _queue.Post(line);
    }

    /// <summary>
    /// The task executed by the <see cref="ActionBlock{String}"/> to write each 
    /// submitted batch of <paramref name="lines"/> to the underlying file, in the order received.
    /// </summary>
    async Task WriteBatch(string[] lines)
    {
        try
        {
            for (int i = 0; i < lines.Length; i++)
            {
                 _writer.WriteLine(lines[i]);
            }
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException cancelEx)
        {
            _errors.Add(new Tuple<string[], string>(new string[] { "Cancelling..." }, cancelEx.Message));
        }
        catch (Exception writeEx)
        {
            _errors.Add(new Tuple<string[], string>(lines, writeEx.Message));
        }
    }

    void ThrowIfDisposed()
    {
        if (_alreadyDisposed || _writer == null || _writer.BaseStream?.CanWrite == false)
            throw new ObjectDisposedException(nameof(QueuedBatchFileWriter));
    }

    /// <summary>
    /// Stops listening for input, flushes any lines not yet written to the file, and releases the underlying resources.
    /// </summary>
    public void Dispose()
    {
        if (_alreadyDisposed)
            return;
        _alreadyDisposed = true;

        try
        {
            // tell the dataflow block there is no more input
            _queue.Complete();
            // wait for the data block to complete
            _batchWriter.Completion.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException cancelEx)
        {
            _errors.Add(new Tuple<string[], string>(new string[] { "Cancelling..." }, cancelEx.Message));
        }
        catch (Exception ex)
        {
            _errors.Add(new Tuple<string[], string>(new[] { "Error waiting for completion of queue." }, ex.Message));
        }

        // flush the writer
        try { _writer.Flush(); }
        catch { }
        // close the writer and file stream
        try { _writer.Dispose(); }
        catch { }
    }

    /// <summary>
    /// Asynchronously stops listening for input, flushes any lines not yet written to the file, and releases the underlying resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_alreadyDisposed)
            return;
        _alreadyDisposed = true;

        try
        {
            // tell the dataflow block there is no more input
            _queue.Complete();
            // wait for the data block to complete
            await _batchWriter.Completion.ConfigureAwait(false);

        }
        catch (Exception ex)
        {
            _errors.Add(new Tuple<string[], string>(new[] { "Error waiting for completion of queue." }, ex.Message));
        }

        // flush the writer
        try { await _writer.FlushAsync().ConfigureAwait(false); }
        catch { }
        // close the writer and file stream
        try { await ((IAsyncDisposable)_writer).DisposeAsync().ConfigureAwait(false); }
        catch { }
    }
}
