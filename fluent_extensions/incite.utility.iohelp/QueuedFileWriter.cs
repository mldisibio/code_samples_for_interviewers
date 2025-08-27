using System.Threading.Channels;

namespace contoso.utility.iohelp;

/// <summary>
/// Enables a stream of messages to be written to the underlying file
/// one at a time, in the order received, without blocking the calling thread.
/// </summary>
public sealed class QueuedFileWriter : IAsyncDisposable, IDisposable
{
    readonly List<Tuple<string?, string>> _errors = new List<Tuple<string?, string>>();
    readonly ChannelWriter<string?> _queueWriter;
    readonly Task _consumerTask;
    readonly StreamWriter _writer;
    bool _alreadyDisposed;

    QueuedFileWriter(StreamWriter writer, CancellationToken cancelToken)
    {
        _writer = writer;
        // create unbounded queue to receive log events
        var queue = Channel.CreateUnbounded<string?>(new UnboundedChannelOptions { SingleReader = true });
        _queueWriter = queue.Writer;
        // start the task which listens for and processes LogEvents
        _consumerTask = StartConsumer(queue.Reader, cancelToken);

    }

    /// <summary>
    /// Returns an instance of <see cref="QueuedFileWriter"/> which enables a stream of messages
    /// to be written to <paramref name="filePath"/> without blocking the current thread.
    /// <see cref="QueuedFileWriter"/> implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>
    /// and must be disposed when finished to flush the queued messages to the file and to release underlying resources.
    /// </summary>
    public static QueuedFileWriter CreateOver(string filePath, CancellationToken cancelToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(paramName: nameof(filePath));

        string fullpath = Path.GetFullPath(filePath).WithParentDirectoryCreation();
        var writer = new StreamWriter(fullpath);
        return new QueuedFileWriter(writer, cancelToken);
    }

    /// <summary>
    /// Returns the collection of errors, if any, with each item containing the line that failed and the exception message.
    /// This collection is available even after closing the writer if the instance has not been dereferenced.
    /// </summary>
    public IEnumerable<Tuple<string?, string>> ErrorMessages => _errors;

    /// <summary>True if one or more submitted strings were not written to disk and an error was encountered.</summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Enqueues <paramref name="line"/> to be written to the underlying file
    /// without blocking the current thread.
    /// </summary>
    /// <param name="line">
    /// The full line to write to the file. If null, an empty line will be written.
    /// </param>
    public void Enqueue(string? line)
    {
        ThrowIfDisposed();
        _queueWriter.TryWrite(line);
    }

    async Task StartConsumer(ChannelReader<string?> reader, CancellationToken cancelToken)
    {
        while (await reader.WaitToReadAsync(cancelToken).ConfigureAwait(false))
        {
            // keep this part synchronous; and consume as many events as possible before returning to WaitToRead
            while (reader.TryRead(out string? msg))
                _writer.WriteLine(msg);
            // we've emptied the queue;
            // flush to disk
            _writer.Flush();
        }
    }

    void ThrowIfDisposed()
    {
        if (_alreadyDisposed || _writer == null || _writer.BaseStream?.CanWrite == false)
            throw new ObjectDisposedException(nameof(QueuedFileWriter));
    }

    /// <summary>
    /// Stops listening for input, flushes any lines not yet written to the file, and releases the underlying resources.
    /// </summary>
    internal void Dispose(bool managed)
    {
        if (_alreadyDisposed)
            return;
        _alreadyDisposed = true;

        if (managed)
        {
            _queueWriter.TryComplete();
            // block synchronously on reader task
            if (_consumerTask != null)
                try
                {
                    _consumerTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException cancelEx)
                {
                    _errors.Add(new Tuple<string?, string>("Cancelling...", cancelEx.Message));
                }
                catch (Exception ex)
                {
                    _errors.Add(new Tuple<string?, string>("Error waiting for completion of queue.", ex.Message));
                }

            // flush the writer
            try { _writer.Flush(); }
            catch { }
            // close the writer and file stream
            try { _writer.Dispose(); }
            catch { }
        }
    }

    /// <summary>
    /// Asynchronously stops listening for input, flushes any lines not yet written to the file, and releases the underlying resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_alreadyDisposed)
            return;
        _alreadyDisposed = true;

        _queueWriter.TryComplete();
        // wait for reader to complete
        if (_consumerTask != null)
            try
            {
                await _consumerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException cancelEx)
            {
                _errors.Add(new Tuple<string?, string>("Cancelling...", cancelEx.Message));
            }
            catch (Exception ex)
            {
                _errors.Add(new Tuple<string?, string>("Error waiting for completion of queue.", ex.Message));
            }

        // flush the writer
        try { _writer.Flush(); }
        catch { }
        // close the writer and file stream
        try { _writer.Dispose(); }
        catch { }
    }

    /// <summary>Marks the queue as closed to more input, and waits for any events remaining in the queue to be processed.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }

}
