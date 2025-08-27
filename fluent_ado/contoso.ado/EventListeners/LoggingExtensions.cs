using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace contoso.ado
{
    /// <summary>LoggingExtensions.</summary>
    public static class LoggingExtensions
    {
        readonly static string[] _crSplit = new[] { "\n", "\r\n" };
        readonly static string _HR = new string('-', 160);
        readonly static Regex _allWhitespace = new Regex(@"\s+", RegexOptions.Compiled);
        const int _singleDebugLine = 160;
        const int _shortParamLength = 80;

        //readonly static Dictionary<string, string> contextLookup = new Dictionary<string, string>(64);

        /// <summary>Returns the full connection string (sans password) and possibly even a connectionId.</summary>
        public static string ToVerboseDebugString(this DbConnection connection)
        {
            if (connection != null)
            {
                DbConnection dbConn = connection;
                try
                {
                    // remove any password
                    var csBuilder = new DbConnectionStringBuilder() { ConnectionString = dbConn.ConnectionString };
                    csBuilder.Remove("password");
                    return csBuilder.ConnectionString;
                }
                catch { }
            }
            return "[Connection N/A]";

        }

        /// <summary>Returns a debug string with server and database names.</summary>
        public static string ToMinimalDebugString(this DbConnection connection)
        {
            if (connection != null)
            {
                DbConnection dbConn = connection;
                try
                {
                    return $"{dbConn.DataSource}.{dbConn.Database}";
                }
                catch { }
            }
            return "[Connection N/A]";

        }

        /// <summary>Returns a string representation of the DbCommand, including connection, command text, and parameter values.</summary>
        /// <param name="command">The <see cref="DbCommand"/> to log.</param>
        /// <param name="verbose">True for parameters to written out fully, as supplied. False to shorten them to one line, with an ellipse if necessary.</param>
        public static string ToDebugString(this DbCommand command, bool verbose = false)
        {
            if (command != null)
            {
                DbCommand dbCmd = command;
                try
                {
                    var paramStrings = dbCmd.Parameters.ToDebugStrings(verbose).ToArray();
                    string paramSep = paramStrings.Sum(s => s.Length) > _singleDebugLine || paramStrings.Any(s => s.IndexOf((char)10) > -1) ? "\r\n" : ", ";
                    string paramsInfoFinal = string.Join(paramSep, paramStrings);

                    string? cmdTxtSingle = dbCmd.CommandText != null && dbCmd.CommandText.IndexOf((char)10) > -1 ? _allWhitespace.Replace(dbCmd.CommandText, " ") : dbCmd.CommandText;
                    string? cmdTxtFinal = cmdTxtSingle != null && cmdTxtSingle.Length > _singleDebugLine ? dbCmd.CommandText : cmdTxtSingle;

                    if ((paramsInfoFinal.Length + cmdTxtFinal?.Length) <= _singleDebugLine)
                        return $"{cmdTxtFinal} {paramsInfoFinal}";
                    else
                        return $"\r\n{cmdTxtFinal}\r\n\r\n{paramsInfoFinal}";

                }
                catch { }
            }
            return "[Command N/A]";

        }

        static IEnumerable<string> ToDebugStrings(this DbParameterCollection dbParams, bool verbose = false)
        {
            if (dbParams != null)
            {
                DbParameterCollection paramList = dbParams;
                int paramCnt = paramList.Count;

                for (int i = 0; i < paramCnt; i++)
                {
                    string debugStr;
                    try
                    {
                        var dbParam = paramList[i];
                        string paramVal = verbose ? dbParam.ToVerboseParamValueString() : dbParam.ToMinimalParamValueString();
                        string paramSize = dbParam.Size > 0 ? String.Format("({0})", dbParam.Size) : String.Empty;
                        string paramType = dbParam.DbType.ToString();

                        debugStr = $"{{{dbParam.ParameterName} {paramType}{paramSize}: {paramVal}}}";
                    }
                    catch
                    {
                        debugStr = $"[Parameter({i}) N/A]";
                    }
                    yield return debugStr;
                }
            }
            else
                yield return "[Parameters N/A]";
        }

        static string ToMinimalParamValueString(this DbParameter dbParam)
        {
            if (dbParam != null)
            {
                object? paramValue = dbParam.Value;
                if (paramValue == null)
                    return "[null]";
                if (paramValue == DBNull.Value)
                    return "[dbNull]";
                try
                {
                    string? strValue = paramValue.ToString();
                    if ((strValue?.Length).GetValueOrDefault() == 0)
                        return string.Empty;

                    string paramValSingle = strValue!.IndexOf((char)10) > -1 ? _allWhitespace.Replace(strValue, " ") : strValue;
                    return paramValSingle.Length > _shortParamLength ? $"{paramValSingle[.._shortParamLength]}..." : paramValSingle;
                }
                catch { }
            }
            return "[N/A]";
        }

        static string ToVerboseParamValueString(this DbParameter dbParam)
        {
            if (dbParam != null)
            {
                object? paramValue = dbParam.Value;
                if (paramValue == null)
                    return "[null]";
                if (paramValue == DBNull.Value)
                    return "[dbNull]";
                try
                {
                    string? strValue = paramValue.ToString();
                    if (strValue is null || strValue.Length == 0)
                        return string.Empty;

                    // if value is multi-line, print on newline;
                    return strValue.IndexOf((char)10) > -1 ? String.Format("\r\n{0}\r\n", strValue) : strValue;
                }
                catch { }
            }
            return "[N/A]";
        }


        /// <summary>Create a minimal debug string from an exception by reducing the StackTrace to what is relevant.</summary>
        public static string? ToMinimalError(this Exception ex, string? message = null) // , [CallerMemberName] string? methodName = null
        {
            // Parse the StackTrace only if there is an exception
            if (ex != null)
            {
                string exMessages = ex.UnwindToString("\r\n--> ");
                string fullExInfo = $"{ex.GetType().Name}: {exMessages}";
                string? trace = ex.StackTrace;
                string? minimalTrace = null;

                if (!string.IsNullOrEmpty(trace))
                {
                    string[] traceLines = trace.Split(_crSplit, StringSplitOptions.None);

                    // Remove uninformative lines from stack trace...ones starting with 'at System.xyz...'
                    var importantContent = traceLines.Where(line => line.IndexOf("at System.") == -1);
                    importantContent = importantContent.Where(line => line.IndexOf("End of stack trace from previous location") == -1);
                    // Remove trace to the validation extension _Arg.Assert
                    importantContent = importantContent.Where(line => line.IndexOf("ParamCheck.Assert") == -1);
                    // Remove trace to the validation extension ThrowOnInvalidRequestState
                    importantContent = importantContent.Where(line => line.IndexOf("ThrowOnInvalidRequestState") == -1);

                    minimalTrace = string.Join(Environment.NewLine, importantContent);
                }

                return $"{message}\r\n{fullExInfo}\r\n{minimalTrace}\r\n{_HR}";
            }
            else
                return message;
        }
    }
}
