using System;
using contoso.sqlite.raw.internals;
using contoso.sqlite.raw.stringutils;

namespace contoso.sqlite.raw
{
    /// <summary>Default console logger implementing <see cref="ISqliteLogger"/>.</summary>
    public sealed class SqliteConsoleLogger : ISqliteLogger
    {
        readonly ConsoleColor _consoleFg;
        readonly ConsoleColor _previousFg;
        const string _connectionHistoryMsg = "Connection {0}. [Opened: {1:D3} Closed: {2:D3}]";

        /// <summary>Initializes a default instance of the <see cref="SqliteConsoleLogger" /> class. Console foreground is white.</summary>
        public SqliteConsoleLogger() : this(ConsoleColor.White) { }

        /// <summary>Initializes a new instance of the <see cref="SqliteConsoleLogger" /> class with a specified foreground color.</summary>
        /// <param name="fgColor">Foreground color for the console logger.</param>
        public SqliteConsoleLogger(ConsoleColor fgColor)
        {
            this._previousFg = Console.ForegroundColor;
            this._consoleFg = fgColor;
        }

        /// <summary>A database connection was opened.</summary>
        /// <param name="opened">Cummulative count of connections opened.</param>
        /// <param name="closed">Cummulative count of connections closed.</param>
        public void ConnectionOpened(int opened, int closed) => TraceDebug(_connectionHistoryMsg, "opened", opened, closed);

        /// <summary>A database connection was closed.</summary>
        /// <param name="opened">Cummulative count of connections opened.</param>
        /// <param name="closed">Cummulative count of connections closed.</param>
        public void ConnectionClosed(int opened, int closed) => TraceDebug(_connectionHistoryMsg, "closed", opened, closed);

        /// <summary>A new database connection failed to open.</summary>
        /// <param name="connDesc">A connection string setting's name, value, or other identifier.</param>
        /// <param name="exception">The exception thrown when the connection failed.</param>
        public void ConnectionFailed(string? connDesc, SqliteDbException exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{Timestamp()}: Failed to open connection {connDesc}");
            string msgList = string.Join(Environment.NewLine, exception.ToMessageCollection());
            Console.WriteLine(msgList);
            Console.ForegroundColor = _previousFg;
        }

        /// <summary>
        /// A command, which was already initiated at the supplied time,
        /// has failed to complete, with the given exception.
        /// </summary>
        /// <param name="commandText">The text of the command that failed its execution.</param>
        /// <param name="exception">The exception thrown when the command failed.</param>
        public void CommandFailed(string? commandText, SqliteDbException exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{Timestamp()}: Failed to execute command:{commandText}");
            string msgList = string.Join(Environment.NewLine, exception.ToMessageCollection());
            Console.WriteLine(msgList);
            Console.ForegroundColor = _previousFg;
        }

        /// <summary>A command, which was already initiated at the supplied time, has now finished.</summary>
        /// <param name="commandText">The text of the command or other description.</param>
        /// <param name="msElapsed">Optional peformance timing in milliseconds elapsed for command to execute.</param>
        public void CommandExecuted(string? commandText, long? msElapsed = null)
        {
            Console.ForegroundColor = this._consoleFg;
            if (msElapsed.HasValue)
                Console.WriteLine($"{Timestamp()}: Executed command [{msElapsed.Display()}]:{FlattenedAbbreviated(commandText)}");
            else
                Console.WriteLine($"{Timestamp()}: Executed command:{FlattenedAbbreviated(commandText)}");
            Console.ForegroundColor = _previousFg;
        }

        /// <summary>Generic Debug level event for logging fine-grained informational events that are most useful to debug an application.</summary>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        public void TraceDebug(string format, params object[] args)
        {
            if (format != null)
            {
                Console.ForegroundColor = this._consoleFg;
                Console.Write($"{Timestamp()}: ");
                Console.WriteLine(format, args);
                Console.ForegroundColor = _previousFg;
            }
        }

        /// <summary>Generic Info level event for logging informational messages that highlight the progress of the application at coarse-grained level.</summary>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        public void TraceInfo(string format, params object[] args)
        {
            if (format != null)
            {
                Console.ForegroundColor = this._consoleFg;
                Console.Write($"{Timestamp()}: ");
                Console.WriteLine(format, args);
                Console.ForegroundColor = _previousFg;
            }
        }

        /// <summary>Generic Error level event for logging error events that might still allow the application to continue running.</summary>
        /// <param name="exception">The exception generated at this event.</param>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        public void TraceError(SqliteDbException? exception, string? format, params object[] args)
        {
            if (!(exception == null && format == null))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"{Timestamp()}: ");
                if (format != null)
                    Console.WriteLine(format, args);
                if (exception != null)
                {
                    string msgList = String.Join(Environment.NewLine, exception.ToMessageCollection());
                    Console.WriteLine(msgList);
                }
                Console.ForegroundColor = _previousFg;
            }
        }

        static string Timestamp() => DateTime.Now.ToString("HH:mm:ss.fff");

        static string FlattenedAbbreviated(string? sql) => sql.NewlinesToSpace(includeTabs: true).TrimConsecutiveSpaces().Left(256).ToString();
    }
}
