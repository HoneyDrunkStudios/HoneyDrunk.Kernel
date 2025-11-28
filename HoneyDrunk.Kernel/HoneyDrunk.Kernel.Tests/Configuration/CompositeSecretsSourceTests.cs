using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Configuration;
using HoneyDrunk.Kernel.Configuration.Secrets;

namespace HoneyDrunk.Kernel.Tests.Configuration;

public class CompositeSecretsSourceTests
{
    [Fact]
    public void Constructor_CreatesCompositeSource()
    {
        var sources = new[] { new TestSecretsSource() };

        var composite = new CompositeSecretsSource(sources);

        composite.Should().NotBeNull();
    }

    [Fact]
    public void TryGetSecret_NoSources_ReturnsFalse()
    {
        var composite = new CompositeSecretsSource([]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetSecret_FirstSourceHasSecret_ReturnsTrue()
    {
        var source1 = new TestSecretsSource { { "key1", "value1" } };
        var source2 = new TestSecretsSource { { "key2", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key1", out var value);

        result.Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void TryGetSecret_SecondSourceHasSecret_ReturnsTrue()
    {
        var source1 = new TestSecretsSource { { "key1", "value1" } };
        var source2 = new TestSecretsSource { { "key2", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key2", out var value);

        result.Should().BeTrue();
        value.Should().Be("value2");
    }

    [Fact]
    public void TryGetSecret_NoSourceHasSecret_ReturnsFalse()
    {
        var source1 = new TestSecretsSource { { "key1", "value1" } };
        var source2 = new TestSecretsSource { { "key2", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key3", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetSecret_MultipleSourcesHaveSameKey_ReturnsFirstMatch()
    {
        var source1 = new TestSecretsSource { { "key", "value1" } };
        var source2 = new TestSecretsSource { { "key", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void TryGetSecret_FirstSourceThrows_TriesNextSource()
    {
        var source1 = new ThrowingSecretsSource();
        var source2 = new TestSecretsSource { { "key", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeTrue();
        value.Should().Be("value2");
    }

    [Fact]
    public void TryGetSecret_AllSourcesThrow_ReturnsFalse()
    {
        var source1 = new ThrowingSecretsSource();
        var source2 = new ThrowingSecretsSource();
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetSecret_MiddleSourceThrows_SkipsToNext()
    {
        var source1 = new TestSecretsSource { { "key1", "value1" } };
        var source2 = new ThrowingSecretsSource();
        var source3 = new TestSecretsSource { { "key2", "value3" } };
        var composite = new CompositeSecretsSource([source1, source2, source3]);

        var result = composite.TryGetSecret("key2", out var value);

        result.Should().BeTrue();
        value.Should().Be("value3");
    }

    [Fact]
    public void TryGetSecret_SourceReturnsNull_ContinuesToNext()
    {
        var source1 = new TestSecretsSource();
        var source2 = new TestSecretsSource { { "key", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeTrue();
        value.Should().Be("value2");
    }

    [Fact]
    public void TryGetSecret_MultipleCalls_QueriesSourcesEachTime()
    {
        var source = new TestSecretsSource { { "key1", "value1" }, { "key2", "value2" } };
        var composite = new CompositeSecretsSource([source]);

        composite.TryGetSecret("key1", out var value1);
        composite.TryGetSecret("key2", out var value2);

        value1.Should().Be("value1");
        value2.Should().Be("value2");
    }

    [Fact]
    public void TryGetSecret_SourceOrderMatters()
    {
        var highPriority = new TestSecretsSource { { "key", "high-priority" } };
        var lowPriority = new TestSecretsSource { { "key", "low-priority" } };

        var composite1 = new CompositeSecretsSource([highPriority, lowPriority]);
        var composite2 = new CompositeSecretsSource([lowPriority, highPriority]);

        composite1.TryGetSecret("key", out var value1);
        composite2.TryGetSecret("key", out var value2);

        value1.Should().Be("high-priority");
        value2.Should().Be("low-priority");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TryGetSecret_NullOrEmptyKey_ReturnsFalse(string? key)
    {
        var source = new TestSecretsSource { { "valid-key", "value" } };
        var composite = new CompositeSecretsSource([source]);

        var result = composite.TryGetSecret(key!, out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetSecret_SourceReturnsEmptyString_ReturnsEmptyString()
    {
        var source = new TestSecretsSource { { "key", string.Empty } };
        var composite = new CompositeSecretsSource([source]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeTrue();
        value.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetSecret_SourceReturnsWhitespace_ReturnsWhitespace()
    {
        var source = new TestSecretsSource { { "key", "   " } };
        var composite = new CompositeSecretsSource([source]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeTrue();
        value.Should().Be("   ");
    }

    [Fact]
    public void TryGetSecret_ManySourcesFirstHasValue_DoesNotCheckRemaining()
    {
        var checkedSources = new List<string>();
        var source1 = new TrackingSecretsSource("source1", checkedSources) { { "key", "value1" } };
        var source2 = new TrackingSecretsSource("source2", checkedSources) { { "key", "value2" } };
        var source3 = new TrackingSecretsSource("source3", checkedSources) { { "key", "value3" } };
        var composite = new CompositeSecretsSource([source1, source2, source3]);

        composite.TryGetSecret("key", out _);

        checkedSources.Should().Equal("source1");
    }

    [Fact]
    public void TryGetSecret_FirstSourceReturnsNull_ChecksNextSource()
    {
        var checkedSources = new List<string>();
        var source1 = new TrackingSecretsSource("source1", checkedSources);
        var source2 = new TrackingSecretsSource("source2", checkedSources) { { "key", "value2" } };
        var composite = new CompositeSecretsSource([source1, source2]);

        composite.TryGetSecret("key", out _);

        checkedSources.Should().Equal("source1", "source2");
    }

    [Fact]
    public void TryGetSecret_CaseInsensitiveKey_UsesExactMatch()
    {
        var source = new TestSecretsSource { { "Key", "value-upper" }, { "key", "value-lower" } };
        var composite = new CompositeSecretsSource([source]);

        composite.TryGetSecret("key", out var valueLower);
        composite.TryGetSecret("Key", out var valueUpper);

        valueLower.Should().Be("value-lower");
        valueUpper.Should().Be("value-upper");
    }

    [Fact]
    public void Constructor_NullSources_ThrowsArgumentNullException()
    {
        var act = () => new CompositeSecretsSource(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetSecret_WithSingleSource_WorksCorrectly()
    {
        var source = new TestSecretsSource { { "key", "value" } };
        var composite = new CompositeSecretsSource([source]);

        var result = composite.TryGetSecret("key", out var value);

        result.Should().BeTrue();
        value.Should().Be("value");
    }

    private sealed class TestSecretsSource : Dictionary<string, string>, ISecretsSource
    {
        public bool TryGetSecret(string key, out string? value)
        {
            return TryGetValue(key, out value);
        }
    }

    private sealed class ThrowingSecretsSource : ISecretsSource
    {
        public bool TryGetSecret(string key, out string? value)
        {
            throw new InvalidOperationException("Source unavailable");
        }
    }

    private sealed class TrackingSecretsSource(string name, List<string> checkedSources) : Dictionary<string, string>, ISecretsSource
    {
        private readonly string _name = name;
        private readonly List<string> _checkedSources = checkedSources;

        public bool TryGetSecret(string key, out string? value)
        {
            _checkedSources.Add(_name);
            return TryGetValue(key, out value);
        }
    }
}
