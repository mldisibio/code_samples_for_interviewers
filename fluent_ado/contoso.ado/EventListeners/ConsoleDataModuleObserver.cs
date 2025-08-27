using System;

namespace contoso.ado.EventListeners
{
    /// <summary>
    /// The Ado Framework logging is log agnostic. The <see cref="IDataModuleObserver"/>
    /// interface allows any listener to be hooked in. This class tells the event listener to write to the Console.
    /// </summary>
    public sealed class ConsoleDataModuleObserver : IDataModuleObserver
    {
        readonly ConsoleColor _consoleFg;
        readonly ConsoleColor _previousFg;

        /// <summary>Initializes a default instance of the <see cref="ConsoleDataModuleObserver" /> class. Console foreground is white.</summary>
        public ConsoleDataModuleObserver() : this(ConsoleColor.White) { }

        /// <summary>Initializes a new instance of the <see cref="ConsoleDataModuleObserver" /> class with a specified foreground color.</summary>
        /// <param name="fgColor">Foreground color for the console logger.</param>
        public ConsoleDataModuleObserver(ConsoleColor fgColor)
        {
            this._previousFg = Console.ForegroundColor;
            this._consoleFg = fgColor;
        }

        /// <summary>A new database connection failed to open.</summary>
        /// <param name="connDesc">A connection string setting's name, value, or other identifier.</param>
        /// <param name="exception">The exception thrown when the connection failed.</param>
        public void ConnectionFailed(string? connDesc, Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{Timestamp()}: Failed to open connection {connDesc}");
            string msgList = string.Join("\r\n", exception.ToMessageCollection());
            Console.WriteLine(msgList);
            Console.ForegroundColor = _previousFg;
        }

        /// <summary>
        /// A command, which was already initiated at the supplied time,
        /// has failed to complete, with the given exception.
        /// </summary>
        /// <param name="commandText">The text of the command that failed its execution.</param>
        /// <param name="exception">The exception thrown when the command failed.</param>
        public void CommandFailed(string? commandText,Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{Timestamp()}: Failed to execute command:{commandText}");
            string msgList = string.Join("\r\n", exception.ToMessageCollection());
            Console.WriteLine(msgList);
            Console.ForegroundColor = _previousFg;
        }

        /// <summary>A command, which was already initiated at the supplied time, has now finished.</summary>
        /// <param name="commandText">The text of the command or other description.</param>
        public void CommandExecuted(string? commandText)
        {
            Console.ForegroundColor = this._consoleFg;
            Console.WriteLine($"{Timestamp()}: Executed command:{commandText}");
            Console.ForegroundColor = _previousFg;
        }

        /// <summary>Generic Debug level event for logging fine-grained informational events that are most useful to debug an application.</summary>
        /// <param name="format">The event description or message format string.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        public void TraceDebug(string format, params object[] args)
        {
            if(format != null)
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
            if(format != null)
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
        public void TraceError(Exception? exception, string? format, params object[] args)
        {
            if(!(exception == null && format == null))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"{Timestamp()}: ");
                if (format != null)
                    Console.WriteLine(format, args);
                if(exception != null)
                {
                    string msgList = String.Join("\r\n", exception.ToMessageCollection());
                    Console.WriteLine(msgList);
                }
                Console.ForegroundColor = _previousFg;
            }
        }

        static string Timestamp()=> DateTime.Now.ToString("HH:mm:ss.fff");

    }
}
