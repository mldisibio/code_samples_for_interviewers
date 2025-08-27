using System;
using System.Collections.Generic;
using System.Linq;

namespace contoso.sqlite.raw
{
    /// <summary>Exception helper methods</summary>
    internal static class ExceptionHelp
    {
        /// <summary>Concatenates the Message contents from one or more nested exceptions in reverse order.</summary>
        public static string UnwindToString(this Exception ex, string separator = " ==> ")
        {
            return string.Join(separator, ex.Unwind());
        }

        /// <summary>Return the collection of nested messages in reverse order, such that the innermost message is first.</summary>
        public static IEnumerable<string> ToMessageCollection(this Exception exception)
        {
            if (exception is AggregateException aggEx)
                return aggEx.Flatten().InnerExceptions.Reverse().SelectMany(ex => ex.Unwind().Reverse());
            else
                return exception.Unwind().Reverse();
        }

        /// <summary>Return the collection of nested messages.</summary>
        static IEnumerable<string> Unwind(this Exception ex)
        {
            Exception? currentEx = ex;
            while (currentEx != null)
            {
                string msg = currentEx.ExtractMessage();
                currentEx = currentEx.InnerException;
                yield return msg;
            }
        }

        static string ExtractMessage(this Exception? ex) => (ex?.Message).IsNullOrEmptyString() ? ex?.GetType()?.Name ?? "exception" : ex.Message;
    }
}
