using contoso.utility.fluentextensions;
using contoso.utility.iohelp;

namespace contoso.decaf.wrapper.singlefile;
/// <summary>Configuration for invoking logRfBin2Csv as a local system process.</summary>
internal class SingleFileDecafConfig
{
    CancellationTokenSource _timeoutCancellationSource;

    /// <summary>Full path of the input file for which 'logRfBin2Csv' will be invoked.</summary>
    public string InputPathOfCaf2 { get; set; }
    /// <summary>Full path to which the decaff'ed csv output file will be moved.</summary>
    public string FinalOutputPathOfCsv { get; set; }
    /// <summary>Full path to the logRfBin2Csv executable.</summary>
    public string PathToExecutable { get; set; }
    /// <summary>True to ignore file errors and try to continue decompression, otherwise false.</summary>
    public bool IgnoreErrors { get; set; }
    /// <summary>Timeout, in milliseconds, for which to wait for any one file to be decaffed. Default is ten minutes. Set to zero for no timeout.</summary>
    public int TimeoutMs { get; set; } = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

    internal CancellationToken GetTimeoutToken()
    {
        if (_timeoutCancellationSource == null)
        {
            if (TimeoutMs > 0)
                // a cancellation source that will cancel a long running single file decaf operation after the specified timeout
                _timeoutCancellationSource = new CancellationTokenSource(TimeoutMs);
            else
                // a cancellation source that is essentially 'no timeout' because the token will not be invoked
                _timeoutCancellationSource = new CancellationTokenSource();
        }
        return _timeoutCancellationSource.Token;
    }

    /// <summary>Ensure configuration is valid.</summary>
    public bool TryEnsureValid(out Exception validityEx)
    {
        validityEx = null;
        try
        {
            if (InputPathOfCaf2.IsNullOrEmptyString())
            {
                validityEx = new FileNotFoundException(message: $"{nameof(InputPathOfCaf2)} is empty");
                return false;
            }

            // get canonical path to input
            InputPathOfCaf2 = Path.GetFullPath(InputPathOfCaf2);
            if (!File.Exists(InputPathOfCaf2))
            {
                validityEx = new FileNotFoundException(message: $"File not found", fileName: InputPathOfCaf2);
                return false;
            }

            // get canonical path to output
            if (FinalOutputPathOfCsv.IsNullOrEmptyString())
                FinalOutputPathOfCsv = $"{InputPathOfCaf2}.csv";
            else
                FinalOutputPathOfCsv = Path.GetFullPath(FinalOutputPathOfCsv);

            // ensure output directory exists
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPathOfCsv)))
            {
                lock (DirectoryDecafConfig.OutputDirectoryLock)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPathOfCsv)))
                        FileSystemHelp.EnsureDirectoryExistsFor(FinalOutputPathOfCsv, out _);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            validityEx = ex;
            return false;
        }
    }
}
