using System;

namespace contoso.sqlite.raw.internals
{
    /// <summary>
    /// Interface that defines a fixed set of logging or performance monitoring events
    /// native to the data layer, which in turn can be handled by any implementing class.
    /// </summary>
    public interface ISqliteLogger
    {
        /// <summary>A database connection was opened.</summary>
        /// <param name="opened">Cummulative count of connections opened.</param>
        /// <param name="closed">Cummulative count of connections closed.</param>
        void ConnectionOpened(int opened, int closed);

        /// <summary>A database connection was closed.</summary>
        /// <param name="opened">Cummulative count of connections opened.</param>
        /// <param name="closed">Cummulative count of connections closed.</param>
        void ConnectionClosed(int opened, int closed);

        /// <summary>A new database connection failed to open.</summary>
        /// <param name="connDesc">A connection string setting's name, value, or other identifier.</param>
        /// <param name="exception">The exception thrown when the connection failed.</param>
        void ConnectionFailed(string? connDesc, SqliteDbException exception);

        /// <summary>A command, initiated at the supplied time, has failed to complete, with the given exception.</summary>
        /// <param name="commandText">The text of the command that failed its execution.</param>
        /// <param name="exception">The exception thrown when the command failed.</param>
        void CommandFailed(string? commandText, SqliteDbException exception);

        /// <summary>A command, which was iinitiated at the supplied time, has now finished.</summary>
        /// <param name="commandText">The text of the command or other description.</param>
        /// <param name="msElapsed">Optional peformance timing in milliseconds elapsed for command to execute.</param>
        void CommandExecuted(string? commandText, long? msElapsed = null);

        /// <summary>Generic Debug level event for logging fine-grained informational events that are most useful to debug an application.</summary>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        void TraceDebug(string format, params object[] args);

        /// <summary>Generic Info level event for logging informational messages that highlight the progress of the application at coarse-grained level.</summary>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        void TraceInfo(string format, params object[] args);

        /// <summary>Generic Error level event for logging error events that might still allow the application to continue running.</summary>
        /// <param name="exception">The exception generated at this event.</param>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        void TraceError(SqliteDbException? exception, string? format, params object[] args);

    }
}
