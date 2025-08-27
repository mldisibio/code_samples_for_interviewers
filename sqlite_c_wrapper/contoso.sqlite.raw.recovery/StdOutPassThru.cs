using System;
using System.Text;

namespace contoso.sqlite.raw.recovery
{
    /// <summary>
    /// Copy the STDOUT from a command line process, discarding messages after a max number of lines,
    /// for cases where output is informational but not something to be processed
    /// </summary>
    public class StdOutPassThru
    {
        readonly StringBuilder _stdOutBuffer = new StringBuilder();
        readonly int _maxLines;
        int _linesReceived;

        /// <summary>Initialize the STDOUT pass-thru with <paramref name="maxLines"/> of messages to record before discarding the rest.</summary>
        public StdOutPassThru(int maxLines = 3)
        {
            _maxLines = maxLines;
            _stdOutBuffer.AppendLine();
        }

        /// <summary>True if any non-empty text was received from STDOUT.</summary>
        public bool HasContent => _linesReceived > 0;

        /// <summary>Handler for any data received from STDOUT.</summary>
        public void RecordLine(string outMsg)
        {
            string? msgTrimmed = NullIfEmptyElseTrimmed(outMsg);
            if (msgTrimmed != null)
            {
                if (_linesReceived++ < _maxLines)
                {
                    _stdOutBuffer.AppendLine(msgTrimmed);
                }
            }
        }

        /// <summary>String representation of the content received plus a summary of total lines.</summary>
        public override string ToString()
        {
            if (HasContent)
            {
                _stdOutBuffer.AppendLine($"...({_linesReceived} lines)");
                return _stdOutBuffer.ToString();
            }
            return string.Empty;
        }

        static string? NullIfEmptyElseTrimmed(string? source) => string.IsNullOrWhiteSpace(source) ? null : source.Trim();

    }
}
