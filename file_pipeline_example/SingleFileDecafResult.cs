using System;
using System.IO;
using System.Text;
using contoso.decaf.wrapper.singlefile;
using contoso.utility.fluentextensions;

namespace contoso.decaf.wrapper
{
    /// <summary>Wraps result of a decaf command line invocation for a single file.</summary>
    public class SingleFileDecafResult
    {
        readonly StringBuilder _errBuffer;
        string _outputMsg;
        bool? _explicitFailure;

        /// <summary>Initialize with the full path to th caf2 file to be decompressed.</summary>
        internal SingleFileDecafResult(SingleFileDecafConfig singleFileConfig)
        {
            _errBuffer = new StringBuilder(128);
            InputCaf2FilePath = singleFileConfig?.InputPathOfCaf2;
            // logRfBin2Csv writes output to input directory, simply appending '.csv' to caf2 file name
            ExpectedOutputCsvFilePath = $"{InputCaf2FilePath}.csv";
        }

        /// <summary>The full path to the caf2 file to be decompressed.</summary>
        public string InputCaf2FilePath { get; }

        /// <summary>Expected csv output full path, whether it exists or not.</summary>
        internal string ExpectedOutputCsvFilePath { get; }

        /// <summary>True if the task was not explicitly marked as failed and the csv output file exists and is not zero bytes.</summary>
        internal bool DecompressedCsvCreated => _explicitFailure.GetValueOrDefault() == false && VerifyIsNonZeroLengthCsv(ExpectedOutputCsvFilePath);

        /// <summary>Expected csv output full path if successfully created.</summary>
        public string FinalOutputCsvFilePath { get; internal set; }

        /// <summary>True if the task was not explicitly marked as failed and the csv output file exists and is not zero bytes.</summary>
        public bool Success => _explicitFailure.GetValueOrDefault() == false && VerifyIsNonZeroLengthCsv(FinalOutputCsvFilePath);

        /// <summary>Message from stdout, if any. Usually just the file name and usually not of interest except for debugging.</summary>
        public string OutputMessage(bool flat = false) => flat ? _outputMsg?.ToString()?.Replace(Environment.NewLine, "|") : _outputMsg?.ToString();

        /// <summary>Error messages, if any, set by wrapper or copied from stderr. Does not necessarily mean the operation failed completely.</summary>
        public string ErrorMessage(bool flat = false) => flat ? _errBuffer?.ToString()?.Replace(Environment.NewLine, "|") : _errBuffer?.ToString();

        /// <summary>Count of error messages received, if any.</summary>
        public short ErrorCount { get; private set; }

        /// <summary>Add or append a message from stdout.</summary>
        internal void AppendOutput(string outputMsg)
        {
            if (outputMsg.IsNotNullOrEmptyString())
            {
                if (_outputMsg == null)
                    _outputMsg = outputMsg;
                else
                    // concatenation acceptable because we really only ever expect one line from stdout
                    _outputMsg = $"{_outputMsg}{Environment.NewLine}{outputMsg}";
            }
        }

        /// <summary>Add or append an error message. A stderr message does not set <see cref="Success"/> to false unless three or more errors are received.</summary>
        internal void AppendError(string errMsg)
        {
            if (errMsg.IsNotNullOrEmptyString())
            {
                _errBuffer.AppendLine(errMsg);
                ErrorCount++;
            }
        }

        /// <summary>Set <see cref="Success"/> false and optionally append <paramref name="errMsg"/> to <see cref="ErrorMessage"/>.</summary>
        internal void SetExplicitFailure(string errMsg = null)
        {
            _explicitFailure = true;
            AppendError(errMsg);
        }

        bool VerifyIsNonZeroLengthCsv(string csvPath)
        {
            if (csvPath.IsNullOrEmptyString())
                return false;
            try
            {
                var csvFileInfo = new FileInfo(csvPath);
                return csvFileInfo.Exists && csvFileInfo.Length > 0;
            }
            catch
            {
                return false;
            }
        }

    }
}
