using FluentAssertions;
using HoneyDrunk.Kernel.Time;

namespace HoneyDrunk.Kernel.Tests.Time;

/// <summary>
/// Tests for <see cref="SystemClock"/>.
/// </summary>
public class SystemClockTests
{
    /// <summary>
    /// Ensures UtcNow returns a reasonable timestamp close to system time.
    /// </summary>
    [Fact]
    public void UtcNow_WhenCalled_ReturnsTimeCloseToSystemTime()
    {
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;
        var now = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        var tolerance = TimeSpan.FromSeconds(5);
        now.Should().BeCloseTo(before, tolerance);
        now.Should().BeOnOrAfter(before);
        now.Should().BeOnOrBefore(after.Add(tolerance));
    }

    /// <summary>
    /// Ensures monotonic increase of high-resolution timestamp.
    /// </summary>
    [Fact]
    public void GetTimestamp_WhenCalledMultipleTimes_ReturnsMonotonicallyIncreasingValues()
    {
        var clock = new SystemClock();
        var a = clock.GetTimestamp();
        var b = clock.GetTimestamp();
        var c = clock.GetTimestamp();

        b.Should().BeGreaterThanOrEqualTo(a);
        c.Should().BeGreaterThanOrEqualTo(b);
    }
}
