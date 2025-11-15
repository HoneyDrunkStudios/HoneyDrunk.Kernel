using FluentAssertions;
using HoneyDrunk.Kernel.Time;

namespace HoneyDrunk.Kernel.Tests.Time;

/// <summary>
/// Tests for <see cref="SystemClock"/>.
/// </summary>
public class SystemClockTests
{
    /// <summary>
    /// Ensures UtcNow is close to the system time.
    /// </summary>
    [Fact]
    public void UtcNow_ShouldBeCloseToSystemTime()
    {
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var now = clock.UtcNow;
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        now.Should().BeOnOrAfter(before);
        now.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Ensures monotonic increase of high-resolution timestamp.
    /// </summary>
    [Fact]
    public void GetTimestamp_ShouldMonotonicallyIncrease()
    {
        var clock = new SystemClock();
        var a = clock.GetTimestamp();
        var b = clock.GetTimestamp();
        var c = clock.GetTimestamp();

        b.Should().BeGreaterThanOrEqualTo(a);
        c.Should().BeGreaterThanOrEqualTo(b);
    }
}
