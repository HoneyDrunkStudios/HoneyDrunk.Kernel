namespace HoneyDrunk.Kernel.Abstractions.Time;

/// <summary>
/// Provides access to the current time in a testable manner.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets a high-resolution timestamp for measuring time intervals.
    /// </summary>
    /// <returns>A monotonically increasing timestamp value.</returns>
    long GetTimestamp();
}
