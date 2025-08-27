using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace contoso.sqlite.raw.internals
{
    /// <summary>
    /// A pass-thru implementation of <see cref="ISqliteLogger"/> that allows this api library to write events
    /// to whichever<see cref="ISqliteLogger"/> the api caller has specified, if any.
    /// </summary>
    /// <remarks>
    /// This implementation assumes one listener per application domain.
    /// </remarks>
    public sealed class ContextLog : ISqliteLogger, IDisposable
    {
        static readonly Lazy<ContextLog> _instance = new Lazy<ContextLog>(() => new ContextLog());
        readonly object _initializationLock = new object();
        ISqliteLogger? _subscriber;
        BlockingCollection<Action>? _queue;
        Task? _listenerLoop;
        bool _isAlreadyDisposed;

        #region Connection Monitoring

        int _connectionsOpened;
        int _connectionsClosed;
        const string _connectionHistoryMsg = "Connection {0}. [Opened: {1:D3} Closed: {2:D3}]";

        #endregion

        ContextLog() { }

        /// <summary>Gets the message queue for the internal api to write to.</summary>
        internal static ContextLog Q { get { return _instance.Value; } }

        /// <summary>Sets the listener if it has not yet been set for the current application domain.</summary>
        internal void SetListener(ISqliteLogger listener)
        {
            if (listener != null)
            {
                lock (_initializationLock)
                {
                    // check that a non-null subscribed listener is set only once
                    if (_subscriber == null)
                    {
                        // create a queue for all messages
                        _queue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
                        // since the queue is simply available indefinitely, use the ProcessExit event to stop any loops or threads and to cleanup
                        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                        // Run the listener on a background thread
                        _listenerLoop = Task.Run(() => ListenerLoop());
                        // set the api-caller supplied listener
                        _subscriber = listener;
                    }
                }
            }
        }

        /// <summary>Converts any database connection state changes of 'Open' or 'Closed' to connection count monitoring messages.</summary>
        internal void ConnectionOpened()
        {
            _connectionsOpened++;
            if(_subscriber != null)
                ConnectionOpened(_connectionsOpened, _connectionsClosed);
            //if (_subscriber != null)
            //    TraceDebug(_connectionHistoryMsg, "opened", _connectionsOpened, _connectionsClosed);
        }

        /// <summary>Converts any database connection state changes of 'Open' or 'Closed' to connection count monitoring messages.</summary>
        internal void ConnectionClosed()
        {
            _connectionsClosed++;
            if (_subscriber != null)
                ConnectionClosed(_connectionsOpened, _connectionsClosed);
            //if (_subscriber != null)
            //    TraceDebug(_connectionHistoryMsg, "closed", _connectionsOpened, _connectionsClosed);
        }

        /// <summary>Allow the api to provide connection counters, even if not logging.</summary>
        internal string GetConnectionHistory(out int opened, out int closed)
        {
            opened = _connectionsOpened;
            closed = _connectionsClosed;
            return string.Format(_connectionHistoryMsg, "history", _connectionsOpened, _connectionsClosed);
        }

        #region Convert API messages into delegates that invoke the client

        /// <summary>Queue the invocation ConnectionOpened on the client listener.</summary>
        public void ConnectionOpened(int opened, int closed)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.ConnectionOpened(opened, closed));
            }
        }

        /// <summary>Queue the invocation ConnectionClosed on the client listener.</summary>
        public void ConnectionClosed(int opened, int closed)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.ConnectionClosed(opened, closed));
            }
        }

        /// <summary>Queue the invocation ConnectionFailed on the client listener.</summary>
        public void ConnectionFailed(string? connDesc, SqliteDbException exception)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.ConnectionFailed(connDesc, exception));
            }
        }

        /// <summary>Queue the invocation CommandFailed on the client listener.</summary>
        public void CommandFailed(string? commandText, SqliteDbException exception)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.CommandFailed(commandText, exception));
            }
        }

        /// <summary>Queue the invocation CommandExecuted on the client listener.</summary>
        /// <remarks>For successful execution, simply flatten and abbreviate the sql text.</remarks>
        public void CommandExecuted(string? commandText, long? msElapsed = null)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.CommandExecuted(commandText, msElapsed));
            }
        }

        /// <summary>Queue the invocation TraceDebug on the client listener.</summary>
        public void TraceDebug(string format, params object[] args)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.TraceDebug(format, args));
            }
        }

        /// <summary>Queue the invocation TraceInfo on the client listener.</summary>
        public void TraceInfo(string format, params object[] args)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.TraceInfo(format, args));
            }
        }

        /// <summary>Queue the invocation TraceError on the client listener.</summary>
        public void TraceError(SqliteDbException? exception, string? format, params object[] args)
        {
            if (_subscriber != null)
            {
                ISqliteLogger obs = _subscriber;
                _queue!.Add(() => obs.TraceError(exception, format, args));
            }
        }

        #endregion

        #region Listener loop that invokes the queued delegates on its own thread

        void ListenerLoop()
        {
            // monitor the queue until the queue signals that nothing more will be added
            while (!_queue!.IsCompleted)
            {
                try
                {
                    // dequeue any pending delegate; if no delegates are in the queue, the 'Take' method blocks here
                    Action invokeListener = _queue.Take();
                    if (invokeListener != null)
                    {
                        // invoke the delegate, and silently swallow any client error
                        try { invokeListener(); }
                        catch { }
                    }
                }
                catch (ObjectDisposedException) { break; } // BlockingCollection was disposed;
                catch (InvalidOperationException) { } // 'IsCompleted' was set to true after we checked but before we called 'Take'; next loop will check and exit
            }
        }

        #endregion

        void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            if (!_isAlreadyDisposed)
                try { Dispose(true); }
                catch { }
        }

        #region IDisposable

        void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {
                    if (_queue != null)
                    {
                        // process is shutting down; inform the queue listener that nothing more will be added
                        try
                        {
                            _queue.CompleteAdding();
                        }
                        catch { }

                        // wait for the listener thread to finish
                        try
                        {
                            _listenerLoop?.GetAwaiter().GetResult();
                        }
                        catch { }

                        // clean-up
                        if (_queue != null)
                            try
                            { _queue.Dispose(); }
                            catch { }
                        _subscriber = null;
                    }
                }
            }
        }

        /// <summary>Attempts to call 'CompleteAdding' on the queue and to wait for the listener loop to end.</summary>
        public void Dispose()
        {
            Dispose(true);
        }


        #endregion
    }
}
