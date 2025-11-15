using FluentAssertions;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.Context;

/// <summary>
/// Tests for <see cref="KernelContext"/>.
/// </summary>
public class KernelContextTests
{
    /// <summary>
    /// Ensures constructor arguments are exposed via properties.
    /// </summary>
    [Fact]
    public void Constructor_ShouldPopulateProperties()
    {
        var corr = "corr";
        var cause = "cause";
        var baggage = new Dictionary<string, string> { ["k"] = "v" };
        var ct = CancellationToken.None;

        var ctx = new KernelContext(corr, cause, baggage, ct);

        ctx.CorrelationId.Should().Be(corr);
        ctx.CausationId.Should().Be(cause);
        ctx.Baggage.Should().BeSameAs(baggage);
        ctx.Cancellation.Should().Be(ct);
    }

    /// <summary>
    /// Ensures BeginScope returns a disposable that is safe to dispose multiple times.
    /// </summary>
    [Fact]
    public void BeginScope_ShouldReturnDisposable_ThatDoesNothing()
    {
        var ctx = new KernelContext("c", null, new Dictionary<string, string>(), default);

        using var scope1 = ctx.BeginScope();
        Action disposeAgain = scope1.Dispose;

        disposeAgain.Should().NotThrow();

        using var scope2 = ctx.BeginScope();
        scope2.Should().NotBeNull();
        scope2.Should().NotBeSameAs(scope1);
    }
}
