using System.Runtime.CompilerServices;
using System.Text;
using contoso.logging.sublog;

namespace contoso.utility.compression;

/// <summary>Wraps operation state and final success or failure.</summary>
public class DecompressToStreamResult
{
    internal DecompressToStreamResult(IOperationLog<IMsgLine> subLog)
    {
        SubLog = subLog;
    }

    /// <summary>True if final state of opertion has been explicitly set to success.</summary>
    public bool Success { get; private set; }

    /// <summary>The error message (or null if no error) submitted when this was set to a failed state.</summary>
    public string? Error { get; private set; }

    /// <summary>The input configuration state for diagnostics or logging.</summary>
    public DecompressInputState InputState { get; internal set; }

    /// <summary>Collection of operation events as a sub-log.</summary>
    public IOperationLog<IMsgLine> SubLog { get; init; }

    /// <summary>Internal continuation flag for each step of operation to check.</summary>
    internal bool Ok { get; private set; } = true;

    /// <summary>Set final state to 'Success' as long as no earlier failure was recorded.</summary>
    internal void SetSuccessful() => Success = Ok;

    /// <summary>Set permanent state to 'Failure'. Expectation is that this is called only once and any operations that follow are a no-op.</summary>
    internal void Fail(string? message = null, Exception? exception = null, [CallerMemberName] string? methodName = "")
    {
        Ok = false;
        Success = false;
        SubLog.Error(message, exception, methodName);
        Error = ComposeErrorMessage(message, exception, methodName);
    }

    string ComposeErrorMessage(string? message, Exception? exception, string? methodName)
    {
        if (exception == null && message.IsNullOrEmptyString())
            return methodName.IsNotNullOrEmptyString() ? string.Concat(methodName, ": Error") : "Unspecified Error";

        var builder = new StringBuilder(256);
        if (methodName.IsNotNullOrEmptyString())
            builder.Append(methodName).Append(": ");

        if (exception != null)
            builder.Append('[').Append(exception.GetType().Name).Append("]: ").Append(exception.Message).Append(' ');

        if (message.IsNotNullOrEmptyString())
            builder.Append(message);

        return builder.ToString();
    }
}
