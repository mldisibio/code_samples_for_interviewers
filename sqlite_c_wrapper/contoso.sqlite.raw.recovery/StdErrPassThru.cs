using System;
using System.Text;

namespace contoso.sqlite.raw.recovery
{
    /// <summary>
    /// Copy the STDERR from a command line process, discarding messages after a max number of lines,
    /// particularly when all that is important to know is that there are errors from the execution of the command.
    /// </summary>
    public class StdErrPassThru
    {
        readonly StringBuilder _stdErrBuffer = new StringBuilder();
        readonly int _maxLines;
        int _linesReceived;

        /// <summary>Initialize the STDERR pass-thru with <paramref name="maxLines"/> errors to record before discarding the rest.</summary>
        public StdErrPassThru(int maxLines = 3)
        {
            _maxLines = maxLines;
            _stdErrBuffer.AppendLine();
        }

        /// <summary>True if any non-empty text was received from STDERR.</summary>
        public bool HasErrors => _linesReceived > 0;

        /// <summary>Handler for any data received from STDERR.</summary>
        public void RecordErrorLine(string errLine)
        {
            string? msgTrimmed = NullIfEmptyElseTrimmed(errLine);
            if (msgTrimmed != null)
            {
                if (_linesReceived++ < _maxLines)
                {
                    _stdErrBuffer.AppendLine(msgTrimmed);
                }
            }
        }

        /// <summary>String representation of the errors received plus a summary of total lines.</summary>
        public override string ToString()
        {
            if (HasErrors)
            {
                _stdErrBuffer.AppendLine($"...({_linesReceived} lines)");
                return _stdErrBuffer.ToString();
            }
            return string.Empty;
        }

        static string? NullIfEmptyElseTrimmed(string? source) => string.IsNullOrWhiteSpace(source) ? null : source.Trim();

    }
}
