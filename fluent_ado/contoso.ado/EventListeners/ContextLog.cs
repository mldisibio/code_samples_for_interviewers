using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using contoso.ado.EventListeners;

namespace contoso.ado
{
    /// <summary>
    /// A pass-thru implementation of <see cref="IDataModuleObserver"/> that allows this api library to write events
    /// to whichever<see cref="IDataModuleObserver"/> the api caller has specified, if any.
    /// </summary>
    public sealed class ContextLog : IDataModuleObserver, IDisposable
    {
        static readonly Lazy<ContextLog> _instance = new Lazy<ContextLog>(() => new ContextLog());

        IDataModuleObserver? _client;
        BlockingCollection<Action>? _queue;
        Task? _listenerLoop;
        bool _isAlreadyDisposed;

        #region Connection Monitoring

        int _connectionsOpened;
        int _connectionsClosed;
        int _mismatchCount;
        const int _possibleLeak = 3;
        const string _connectionOpenedMsg = "Connection opened.";
        const string _connectionClosedMsg = "Connection closed.";
        const string _connectionHistoryMsg = "Connection History: Opened: {0:D3} Closed {1:D3}.";
        const string _connectionMismatchMsg = "Possible connection leak: Opened: {0:D3} Closed {1:D3}.";

        #endregion

        ContextLog()
        {
        }

        /// <summary>Gets the message queue for the internal api to write to.</summary>
        internal static ContextLog Q { get { return _instance.Value; } }

        /// <summary>Sets the listener if it has not yet been set for the current application domain.</summary>
        internal void SetListener(IDataModuleObserver listener)
        {
            if (listener != null)
            {
                // check that a non-null observer is set only once
                if (_client == null)
                {
                    // create a queue for all messages
                    _queue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
                    // since the queue is simply available indefinitely, use the ProcessExit event to stop any loops or threads and to cleanup
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                    // Run the listener on a background thread
                    _listenerLoop = Task.Run(() => ListenerLoop());
                    // set the api-caller supplied listener
                    _client = listener;
                }
            }
        }

        /// <summary>Converts any database connection state changes of 'Open' or 'Closed' to connection count monitoring messages.</summary>
        internal void ConnectionStateChanged(object sender, StateChangeEventArgs e)
        {
            if (_client != null)
            {
                if (e.CurrentState == ConnectionState.Open)
                {
                    _connectionsOpened++;
                    // verbose debugging, capture the opening;
                    TraceDebug(_connectionOpenedMsg);
                }
                else if (e.CurrentState == ConnectionState.Closed)
                {
                    _connectionsClosed++;

                    if (_connectionsOpened > _connectionsClosed)
                    {
                        // some connections have not been closed; this can be expected;
                        _mismatchCount++;
                        if (_mismatchCount > _possibleLeak)
                            // if still a mismatch after multiple closures, note in the log; it might still be a slow query;
                            TraceInfo(_connectionMismatchMsg, this._connectionsOpened, this._connectionsClosed);
                    }
                    else
                    {
                        // open and close now match; did we log a mismatch?
                        if (_mismatchCount > _possibleLeak)
                        {
                            // we probably wrote a warning to the log, so put the person reading the log at ease:
                            TraceInfo(_connectionHistoryMsg, this._connectionsOpened, this._connectionsClosed);
                        }
                        else
                        {
                            // verbose debugging, capture the closure;
                            TraceDebug(_connectionClosedMsg);
                        }
                        // reset the mismatch counter
                        _mismatchCount = 0;
                    }
                }
            }
        }

        #region Convert API messages into delegates that invoke the client

        /// <summary>Queue the invocation ConnectionFailed on the client listener.</summary>
        public void ConnectionFailed(string? connDesc, Exception exception)
        {
            if (_client != null)
            {
                IDataModuleObserver obs = _client;
                _queue!.Add(() => obs.ConnectionFailed(connDesc, exception));
            }
        }

        /// <summary>A new database connection failed to open.</summary>
        /// <param name="conn">The Connection object itself.</param>
        /// <param name="exception">The exception thrown when the connection failed.</param>
        public void ConnectionFailed(DbConnection conn, Exception exception)
        {
            ConnectionFailed(conn.ToVerboseDebugString(), exception);
        }

        /// <summary>Queue the invocation CommandFailed on the client listener.</summary>
        public void CommandFailed(string? commandText, Exception exception)
        {
            if (_client != null)
            {
                IDataModuleObserver obs = _client;
                _queue!.Add(() => obs.CommandFailed(commandText, exception));
            }
        }

        /// <summary>A command, initiated at the supplied time, has failed to complete, with the given exception.</summary>
        /// <param name="command">The <see cref="DbCommand"/> object that failed its execution.</param>
        /// <param name="exception">The exception thrown when the command failed.</param>
        public void CommandFailed(DbCommand command, Exception exception)
        {
            CommandFailed(command.ToDebugString(verbose: true), exception);
        }

        /// <summary>Queue the invocation CommandExecuted on the client listener.</summary>
        public void CommandExecuted(string? commandText)
        {
            if (_client != null)
            {
                IDataModuleObserver obs = _client;
                _queue!.Add(() => obs.CommandExecuted(commandText));
            }
        }

        /// <summary>A command, which was initiated at the supplied time, has now finished.</summary>
        /// <param name="command">The <see cref="DbCommand"/> object.</param>
        public void CommandExecuted(DbCommand command)
        {
                CommandExecuted(command.ToDebugString(verbose: false));
        }

        /// <summary>Queue the invocation TraceDebug on the client listener.</summary>
        public void TraceDebug(string format, params object[] args)
        {
            if (_client != null)
            {
                IDataModuleObserver obs = _client;
                _queue!.Add(() => obs.TraceDebug(format, args));
            }
        }

        /// <summary>Queue the invocation TraceInfo on the client listener.</summary>
        public void TraceInfo(string format, params object[] args)
        {
            if (_client != null)
            {
                IDataModuleObserver obs = _client;
                _queue!.Add(() => obs.TraceInfo(format, args));
            }
        }

        /// <summary>Queue the invocation TraceError on the client listener.</summary>
        public void TraceError(Exception? exception, string? format = null, params object[] args)
        {
            if (_client != null)
            {
                IDataModuleObserver obs = _client;
                _queue!.Add(() => obs.TraceError(exception, format, args));
            }
        }

        #endregion

        #region Listener loop that invokes the queued delegates on its own thread

        void ListenerLoop()
        {
            // monitor the queue until the queue signals that nothing more will be added
            while(_queue != null && !_queue.IsCompleted)
            {
                try
                {
                    // dequeue any pending delegate; if no delegates are in the queue, the 'Take' method blocks here
                    Action invokeClient = _queue.Take();
                    if (invokeClient != null)
                    {
                        // invoke the delegate, and silently swallow any client error
                        try { invokeClient(); }
                        catch { }
                    }
                }
                catch(ObjectDisposedException) { break; } // BlockingCollection was disposed;
                catch(InvalidOperationException) { } // 'IsCompleted' was set to true after we checked but before we called 'Take'; next loop will check and exit
            }
        }

        #endregion

        void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            if(!_isAlreadyDisposed)
                try { Dispose(true); }
                catch { }
        }

        #region IDisposable

        void Dispose(bool isManagedCall)
        {
            if(!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {

                    // process is shutting down; inform the queue listener that nothing more will be added
                    try
                    {
                        _queue?.CompleteAdding();
                    }
                    catch { }

                    // wait for the listener thread to finish
                    if (_listenerLoop != null && !_listenerLoop.IsCompleted)
                        _listenerLoop.Wait(3000);

                    // clean-up
                    if (_queue != null)
                        try
                        { _queue.Dispose(); }
                        catch { }
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
