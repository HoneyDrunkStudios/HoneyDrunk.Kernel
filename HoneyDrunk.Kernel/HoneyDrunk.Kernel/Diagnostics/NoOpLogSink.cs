using HoneyDrunk.Kernel.Abstractions.Diagnostics;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// No-op implementation of ILogSink for testing and fallback scenarios.
/// </summary>
public sealed class NoOpLogSink : ILogSink
{
    /// <inheritdoc />
    public void Write(LogLevel level, string messageTemplate, IReadOnlyDictionary<string, object?> properties, Exception? exception = null)
    {
        // No-op: discard all log entries
    }
}
