using System;
using System.IO;
using System.Threading;
using contoso.logfiles.environment;
using contoso.utility.fluentextensions;

namespace contoso.decaf.wrapper
{
    /// <summary>Configuration for invoking logRfBin2Csv recursively for a given parent directory.</summary>
    public class RecursiveDecafConfig
    {
        internal readonly static object OutputDirectoryLock = new object();
        CancellationTokenSource _timeoutCancellationSource;
        string _pathToExe;

        /// <summary>Full path to the input directory against which which will be recursed for caf2 files.</summary>
        public string StartingRootDirectory { get; set; }
        
        /// <summary>Full path to the output directory to which decaff'ed csv files will be written.</summary>
        public string OutputDirectoryOfCsv { get; set; }

        /// <summary>Option for a prefix/parent directory to each serial number output directory.</summary>
        public OutputPrefixOption OutputPrefix { get; set; }

        /// <summary>Full path to the logRfBin2Csv executable.</summary>
        public string PathToExecutable
        {
            get
            {
                if (_pathToExe.IsNullOrEmptyString())
                {
                    _pathToExe = FindExeInAssemblyDir();
                }
                return _pathToExe;
            }
            set { _pathToExe = value; }
        }

        /// <summary>True to ignore file errors and try to continue decompression, otherwise false.</summary>
        public bool IgnoreErrors { get; set; }
        /// <summary>
        /// Timeout, in milliseconds, for processing all files in all directory, after which the process will be cancelled. Default is 24 hours.
        /// </summary>
        public int TimeoutMs { get; set; } = (int)TimeSpan.FromHours(24).TotalMilliseconds;
        
        /// <summary>Total number of cores to allocate for entire batch job. Default is 1.</summary>
        public short Pfx { get; set; } = 1;

        /// <summary>When set, will still recurse all subdirectories, but will only decaf files found in directories starting with the given string; e.g. "T5A".</summary>
        public string Caf2DirectoryStartsWith { get; set; }

        internal CancellationToken GetTimeoutToken()
        {
            if (_timeoutCancellationSource == null)
            {
                if (TimeoutMs > 0)
                    // a cancellation source that will cancel a long running directory decaf operation after the specified timeout
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
                if (StartingRootDirectory.IsNullOrEmptyString())
                {
                    validityEx = new DirectoryNotFoundException(message: $"{nameof(StartingRootDirectory)} is empty");
                    return false;
                }

                // get canonical path to input
                StartingRootDirectory = Path.GetFullPath(StartingRootDirectory);
                if (!Directory.Exists(StartingRootDirectory))
                {
                    validityEx = new DirectoryNotFoundException(message: $"Directory not found: {StartingRootDirectory}");
                    return false;
                }

                // get canonical path to output
                if (OutputDirectoryOfCsv.IsNullOrEmptyString())
                    OutputDirectoryOfCsv = StartingRootDirectory;
                else
                {
                    OutputDirectoryOfCsv = Path.GetFullPath(OutputDirectoryOfCsv);

                    // ensure the output directory exists
                    if (!Directory.Exists(OutputDirectoryOfCsv))
                    {
                        lock (OutputDirectoryLock)
                        {
                            if (!Directory.Exists(OutputDirectoryOfCsv))
                                Directory.CreateDirectory(OutputDirectoryOfCsv);
                        }
                    }
                }

                string pathToTest = PathToExecutable;
                if (!File.Exists(pathToTest))
                {
                    validityEx = new FileNotFoundException(message: $"logRfBin2Csv executable not found: {pathToTest}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                validityEx = ex;
                return false;
            }
        }

        string FindExeInAssemblyDir()
        {
            bool isLinux = RuntimeEnv.IsLinux;
            string appDomainPath = AppDomain.CurrentDomain.BaseDirectory;
            string candidatePath;
            if (isLinux)
                candidatePath = Path.GetFullPath($"{appDomainPath}/linux/LogRfBin2Csv");
            else
                candidatePath = Path.GetFullPath($"{appDomainPath}\\win\\LogRfBin2Csv.exe");

            return File.Exists(candidatePath) ? candidatePath : null;
        }
    }
}
