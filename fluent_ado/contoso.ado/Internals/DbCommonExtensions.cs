using System;
using System.Data.Common;
using System.Xml;
using contoso.ado.Common;

namespace contoso.ado.Internals
{
    /// <summary>Internal fluent extensions to help with readability and simplicity.</summary>
    public static class DbCommonExtensions
    {
        /// <summary>Inline, chained null check on <see cref="Database"/>.</summary>
        public static Database ThrowIfNull(this Database src)
        {
            if (src == null)
                throw new ArgumentNullException("database", "The 'Database' context is null.");
            return src;
        }

        /// <summary>Inline, chained null check on <see cref="DbCommand"/>.</summary>
        public static DbCommand ThrowIfNull(this DbCommand src)
        {
            if (src == null)
                throw new ArgumentNullException("dbCommand", "The Command instance is null.");
            return src;
        }

        /// <summary>Inline, chained null check on <see cref="DbParameter"/>.</summary>
        public static DbParameter ThrowIfNull(this DbParameter src)
        {
            if (src == null)
                throw new ArgumentNullException("dbParameter", "The Parameter instance is null.");
            return src;
        }

        /// <summary>Check for null before closing and disposing <paramref name="connection"/>.</summary>
        public static void SafeClose(this DbConnection connection)
        {
            try
            { connection?.Close(); }
            catch { }

            try
            { connection?.Dispose(); }
            catch { }
        }

        /// <summary>Check for null before closing and disposing <paramref name="command"/>.</summary>
        public static void SafeDispose(this DbCommand command)
        {
            try
            { command?.Dispose(); }
            catch { }
        }

        /// <summary>True if basic checks against the sql XmlReader confirm it is readable.</summary>
        public static bool CanRead(this XmlReader reader)
        {
            if (reader == null)
                return false;
            if (reader.ReadState == ReadState.Closed || reader.ReadState == ReadState.EndOfFile || reader.ReadState == ReadState.Error)
                return false;
            try
            {
                if (reader.MoveToContent() == XmlNodeType.None)
                    return false;
                if (reader.EOF)
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
