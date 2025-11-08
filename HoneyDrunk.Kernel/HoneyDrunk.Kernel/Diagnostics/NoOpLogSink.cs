using HoneyDrunk.Kernel.Abstractions.Diagnostics;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// No-op implementation of ILogSink that discards all log entries.
/// </summary>
public sealed class NoOpLogSink : ILogSink
{
    /// <inheritdoc />
    public void Write(LogLevel level, string messageTemplate, IReadOnlyDictionary<string, object?> properties, Exception? exception = null)
    {
    }
}
