namespace HoneyDrunk.Kernel.Abstractions.Diagnostics;

/// <summary>
/// Defines log severity levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Verbose debugging information.
    /// </summary>
    Trace,

    /// <summary>
    /// Debugging information.
    /// </summary>
    Debug,

    /// <summary>
    /// Informational messages.
    /// </summary>
    Information,

    /// <summary>
    /// Warning messages for potentially harmful situations.
    /// </summary>
    Warning,

    /// <summary>
    /// Error messages for failures.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error messages for severe failures.
    /// </summary>
    Critical
}

/// <summary>
/// Minimal structured logging contract for capturing log entries.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="level">The severity level of the log entry.</param>
    /// <param name="messageTemplate">The message template with placeholders.</param>
    /// <param name="properties">Named properties for the message template.</param>
    /// <param name="exception">Optional exception associated with the log entry.</param>
    void Write(LogLevel level, string messageTemplate, IReadOnlyDictionary<string, object?> properties, Exception? exception = null);
}
