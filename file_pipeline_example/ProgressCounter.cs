using System.Collections.Concurrent;
using System.Diagnostics;
using contoso.logfiles;
using contoso.logging;
using contoso.utility.iohelp;
using Microsoft.Extensions.Logging;

namespace contoso.decaf.wrapper;

/// <summary>Progress counter.</summary>
public class ProgressCounter
{
    readonly static Lazy<ProgressCounter> _singletonInstance = new Lazy<ProgressCounter>(() => new ProgressCounter());
    readonly static ILogger _log = Logging.CreateLogger<ProgressCounter>();
    readonly Stopwatch _sw;
    readonly string _blanks;
    readonly Process _currentProcess;
    readonly ConcurrentQueue<string> _errorQueue;
    readonly ConcurrentQueue<string> _infoQueue;

    long _maxMemBytes;
    bool _complete;
    bool _swStarted;

    internal ProgressCounter()
    {
        _sw = new Stopwatch();
        _blanks = new string(' ', 16);
        _currentProcess = Process.GetCurrentProcess();
        _errorQueue = new ConcurrentQueue<string>();
        _infoQueue = new ConcurrentQueue<string>();
    }

    /// <summary>Singleton instance of <see cref="ProgressCounter"/> with option to write to console and log</summary>
    public static ProgressCounter Instance => _singletonInstance.Value;

    /// <summary>Count of caf2 pending decompression.</summary>
    public int Caf2Found = 0;
    /// <summary>Count successfully decompressed.</summary>
    public int DecafCsv = 0;
    /// <summary>Count decompression failed</summary>
    public int FailureCount = 0;
    /// <summary>Cummulative bytes decompressed.</summary>
    public long OutputBytes = 0;

    /// <summary>Set true when processing has completed</summary>
    public bool Complete
    {
        get => _complete;
        set
        {
            _complete = value;
            if (_complete)
                _sw.Stop();
        }
    }

    internal void AppendInfo(string logMsg)
    {
        if (!String.IsNullOrWhiteSpace(logMsg))
            _infoQueue.Enqueue(logMsg);
    }

    internal void AppendError(string errMsg)
    {
        if (!String.IsNullOrWhiteSpace(errMsg))
            _errorQueue.Enqueue(errMsg);
    }

    /// <summary>Task to write progress to console and log.</summary>
    /// <param name="consoleMs">Delay in milliseconds for refreshing the console output with progress.</param>
    /// <param name="logMs">Delay in milliseconds for writing a flat progress message to the log.</param>
    public async Task WriteProgress(int consoleMs, int logMs)
    {
        bool finalLoop = false;
        long nextLogMs = logMs;
        if (!_swStarted)
        {
            _sw.Start();
            _swStarted = true;
        }
        Console.WriteLine();
        int currentTop = Console.CursorTop;

        while (!finalLoop)
        {
            ProgressCounter counts = Copy();
            long msElapsed = _sw.ElapsedMilliseconds;
            finalLoop = counts.Complete;

            string input__ = $"{$"{counts.Caf2Found:N0}",13} Caf2 Found";
            string decaf__ = $"{$"{counts.DecafCsv:N0}",13} Decaf Csv";
            string failed_ = $"{$"{counts.FailureCount:N0}",13} Failed";
            string memory_ = GetMemoryUsageDisplay();
            string elapsed = msElapsed.ElapsedDisplay(omitFraction: true);
            string diskuse = $"{$"{counts.OutputBytes.ToFormattedSizeDisplay()}",13} Output";

            // write from same start coordinate on console
            Console.SetCursorPosition(0, currentTop);
            Console.WriteLine($"{elapsed}{_blanks}");
            Console.WriteLine($"{input__}{_blanks}");
            Console.WriteLine($"{decaf__}{_blanks}");
            Console.WriteLine($"{failed_}{_blanks}");
            Console.WriteLine($"{diskuse}{_blanks}");
            Console.WriteLine($"{memory_}{_blanks}");
            Console.WriteLine();

            if (finalLoop)
            {
                EmptyMessageQueuesToLog();
                // write final counts to log
                _log.WithContext().LogInformation($"{elapsed}  {input__}  {decaf__}  {failed_}  {counts.OutputBytes.ToFormattedSizeDisplay()}  {_maxMemBytes.ToFormattedSizeDisplay()} (Max Mem)");
            }
            else
            {
                if (msElapsed > nextLogMs)
                {
                    // log delay has elapsed; write to log
                    EmptyMessageQueuesToLog();
                    _log.WithContext().LogInformation($"{elapsed}  {input__}  {decaf__}  {failed_}  {memory_}");
                    nextLogMs += logMs;
                }
                // reschedule after console delay
                await Task.Delay(consoleMs).ConfigureAwait(false);
            }
        }
    }

    void EmptyMessageQueuesToLog()
    {
        while (_infoQueue.TryDequeue(out string logMsg))
        {
            _log.WithContext(methodName: "FromInfoQueue").LogInformation(logMsg);
        }
        while (_errorQueue.TryDequeue(out string errMsg))
        {
            _log.WithContext(methodName: "FromErrorQueue").LogError(errMsg);
        }
    }

    ProgressCounter Copy()
    {
        return new ProgressCounter
        {
            Caf2Found = this.Caf2Found,
            DecafCsv = this.DecafCsv,
            FailureCount = this.FailureCount,
            OutputBytes = this.OutputBytes,
            Complete = this.Complete
        };
    }


    string GetMemoryUsageDisplay()
    {
        _currentProcess.Refresh();
        long memBytes = _currentProcess.WorkingSet64;
        if (memBytes > _maxMemBytes)
            Interlocked.Exchange(ref _maxMemBytes, memBytes);
        string memDisplay = memBytes.ToFormattedSizeDisplay();
        return $"{ memDisplay,13} WorkingSet64";
    }


}
