using System.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Time;

namespace HoneyDrunk.Kernel.Time;

/// <summary>
/// System clock implementation using system time.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public long GetTimestamp() => Stopwatch.GetTimestamp();
}
