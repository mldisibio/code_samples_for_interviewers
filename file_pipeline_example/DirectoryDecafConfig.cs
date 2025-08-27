using System;
using System.IO;
using System.Threading;
using contoso.logfiles.environment;
using contoso.logfiles.environment.services;
using contoso.utility.fluentextensions;

namespace contoso.decaf.wrapper
{
    /// <summary>Configuration for invoking logRfBin2Csv as a local system process for one directory.</summary>
    public class DirectoryDecafConfig
    {
        internal readonly static object OutputDirectoryLock = new object();
        const string _invalidCaf2DirectoryName = "invalid";
        CancellationTokenSource _timeoutCancellationSource;
        string _pathToExe;

        /// <summary>Full path to the input directory against which 'logRfBin2Csv' will be invoked.</summary>
        public string InputDirectoryOfCaf2 { get; set; }
        /// <summary>Full path to the output directory to which decaff'ed csv files will be written.</summary>
        public string OutputDirectoryOfCsv { get; set; }
        /// <summary>A directory to which invalid source caf2 files will be moved.</summary>
        internal string DirectoryForInvalidCaf2Files { get; private set; }
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
        /// Timeout, in milliseconds, for processing all files in a directory, after which the process will be cancelled. Default is 24 hours.
        /// </summary>
        public int DirectoryTimeoutMs { get; set; } = (int)TimeSpan.FromHours(24).TotalMilliseconds;

        /// <summary>
        /// Timeout, in milliseconds, for processing a single file, after which the process will be cancelled. Default is ten minutes.
        /// </summary>
        public int SingleFileTimeoutMs { get; set; } = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

        /// <summary>Number of single file decaf processes to run in parallel for one directory stream. Default is 1.</summary>
        public short Pfx { get; set; } = 1;

        internal CancellationToken GetTimeoutToken()
        {
            if (_timeoutCancellationSource == null)
            {
                if (DirectoryTimeoutMs > 0)
                    // a cancellation source that will cancel a long running directory decaf operation after the specified timeout
                    _timeoutCancellationSource = new CancellationTokenSource(DirectoryTimeoutMs);
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
                if (InputDirectoryOfCaf2.IsNullOrEmptyString())
                {
                    validityEx = new DirectoryNotFoundException(message: $"{nameof(InputDirectoryOfCaf2)} is empty");
                    return false;
                }

                // get canonical path to input
                InputDirectoryOfCaf2 = Path.GetFullPath(InputDirectoryOfCaf2);
                if (!Directory.Exists(InputDirectoryOfCaf2))
                {
                    validityEx = new DirectoryNotFoundException(message: $"Directory not found: {InputDirectoryOfCaf2}");
                    return false;
                }

                // ensure a directory into which invalid source files will be moved exists
                DirectoryForInvalidCaf2Files = Path.Join(InputDirectoryOfCaf2, _invalidCaf2DirectoryName);
                if (!Directory.Exists(DirectoryForInvalidCaf2Files))
                {
                    lock (OutputDirectoryLock)
                    {
                        if (!Directory.Exists(DirectoryForInvalidCaf2Files))
                            Directory.CreateDirectory(DirectoryForInvalidCaf2Files);
                    }
                }

                // get canonical path to output
                if (OutputDirectoryOfCsv.IsNullOrEmptyString())
                    OutputDirectoryOfCsv = InputDirectoryOfCaf2;
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
            if(isLinux)
                candidatePath = Path.GetFullPath($"{appDomainPath}/linux/LogRfBin2Csv");
            else
                candidatePath = Path.GetFullPath($"{appDomainPath}\\win\\LogRfBin2Csv.exe");

            return File.Exists(candidatePath) ? candidatePath : null;
        }
    }
}
