using FluentAssertions;
using HoneyDrunk.Kernel.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="NoOpLogSink"/>.
/// </summary>
public class NoOpLogSinkTests
{
    /// <summary>
    /// Ensures the sink ignores inputs and does not throw.
    /// </summary>
    [Fact]
    public void Write_WhenCalledWithAnyInput_DoesNotThrow()
    {
        var sink = new NoOpLogSink();
        var props = new Dictionary<string, object?>
        {
            ["orderId"] = 123,
            ["user"] = "alice",
        };

        var act = () => sink.Write(Abstractions.Diagnostics.LogLevel.Information, "Processed order {orderId} for {user}", props, new InvalidOperationException("boom"));
        act.Should().NotThrow();
    }
}
