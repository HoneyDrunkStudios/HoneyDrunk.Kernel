using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Config;
using HoneyDrunk.Kernel.Config.Secrets;

namespace HoneyDrunk.Kernel.Tests.Config.Secrets;

/// <summary>
/// Tests for <see cref="CompositeSecretsSource"/>.
/// </summary>
public class CompositeSecretsSourceTests
{
    /// <summary>
    /// Returns false with null when there are no sources.
    /// </summary>
    [Fact]
    public void TryGetSecret_WithNoSources_ReturnsFalseAndNull()
    {
        var composite = new CompositeSecretsSource([]);
        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Returns the first successful value among sources.
    /// </summary>
    [Fact]
    public void TryGetSecret_ReturnsFirstSuccessfulValue()
    {
        var composite = new CompositeSecretsSource(
        [
            new StaticSecretsSource(false, null),
            new StaticSecretsSource(true, "secret"),
        ]);

        var result = composite.TryGetSecret("Database", out var value);

        result.Should().BeTrue();
        value.Should().Be("secret");
    }

    /// <summary>
    /// Skips sources that throw exceptions and continues.
    /// </summary>
    [Fact]
    public void TryGetSecret_SkipsThrowingSources()
    {
        var composite = new CompositeSecretsSource(
        [
            new ThrowingSecretsSource(new InvalidOperationException()),
            new StaticSecretsSource(true, "ok"),
        ]);

        var result = composite.TryGetSecret("ApiKey", out var value);

        result.Should().BeTrue();
        value.Should().Be("ok");
    }

    /// <summary>
    /// Returns false when no source can provide the value.
    /// </summary>
    [Fact]
    public void TryGetSecret_NoSourceHasKey_ReturnsFalseAndNull()
    {
        var composite = new CompositeSecretsSource(
        [
            new StaticSecretsSource(false, "ignored"),
            new StaticSecretsSource(false, null),
        ]);

        var result = composite.TryGetSecret("Anything", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    private sealed class StaticSecretsSource(bool result, string? value) : ISecretsSource
    {
        private readonly bool result = result;
        private readonly string? storedValue = value;

        public bool TryGetSecret(string key, out string? value)
        {
            value = storedValue;
            return result;
        }
    }

    private sealed class ThrowingSecretsSource(Exception exception) : ISecretsSource
    {
        public bool TryGetSecret(string key, out string? value)
        {
            value = null;
            throw exception;
        }
    }
}
