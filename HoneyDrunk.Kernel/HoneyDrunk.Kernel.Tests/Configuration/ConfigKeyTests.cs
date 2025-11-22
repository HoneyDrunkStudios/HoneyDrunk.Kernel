using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Configuration;

namespace HoneyDrunk.Kernel.Tests.Configuration;

public class ConfigKeyTests
{
    [Theory]
    [InlineData("Database")]
    [InlineData("Database:ConnectionString")]
    [InlineData("App:Settings:MaxRetries")]
    [InlineData("Key")]
    public void Constructor_ValidValue_CreatesConfigKey(string value)
    {
        var configKey = new ConfigKey(value);

        configKey.Value.Should().Be(value);
        configKey.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespace_ThrowsArgumentException(string? value)
    {
        var act = () => new ConfigKey(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Database", null)]
    [InlineData("Database:ConnectionString", "Database")]
    [InlineData("App:Settings:MaxRetries", "App:Settings")]
    [InlineData("A:B:C:D", "A:B:C")]
    public void Parent_ReturnsExpectedParent(string value, string? expectedParent)
    {
        var configKey = new ConfigKey(value);

        var parent = configKey.Parent;

        if (expectedParent == null)
        {
            parent.Should().BeNull();
        }
        else
        {
            parent.Should().NotBeNull();
            parent!.Value.Value.Should().Be(expectedParent);
        }
    }

    [Fact]
    public void Parent_SingleSegment_ReturnsNull()
    {
        var configKey = new ConfigKey("Database");

        var parent = configKey.Parent;

        parent.Should().BeNull();
    }

    [Fact]
    public void Parent_MultipleSegments_ReturnsParent()
    {
        var configKey = new ConfigKey("Database:ConnectionString");

        var parent = configKey.Parent;

        parent.Should().NotBeNull();
        parent!.Value.Value.Should().Be("Database");
    }

    [Theory]
    [InlineData("Database", 1)]
    [InlineData("Database:ConnectionString", 2)]
    [InlineData("App:Settings:MaxRetries", 3)]
    [InlineData("A:B:C:D:E", 5)]
    public void GetSegments_ReturnsExpectedSegments(string value, int expectedCount)
    {
        var configKey = new ConfigKey(value);

        var segments = configKey.GetSegments();

        segments.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void GetSegments_SingleSegment_ReturnsOneSegment()
    {
        var configKey = new ConfigKey("Database");

        var segments = configKey.GetSegments();

        segments.Should().HaveCount(1);
        segments[0].Should().Be("Database");
    }

    [Fact]
    public void GetSegments_MultipleSegments_ReturnsSplit()
    {
        var configKey = new ConfigKey("Database:ConnectionString");

        var segments = configKey.GetSegments();

        segments.Should().HaveCount(2);
        segments[0].Should().Be("Database");
        segments[1].Should().Be("ConnectionString");
    }

    [Fact]
    public void CreateChild_ValidSegment_CreatesChildKey()
    {
        var parent = new ConfigKey("Database");

        var child = parent.CreateChild("ConnectionString");

        child.Value.Should().Be("Database:ConnectionString");
    }

    [Fact]
    public void CreateChild_NestedKey_CreatesGrandchildKey()
    {
        var parent = new ConfigKey("App:Settings");

        var child = parent.CreateChild("MaxRetries");

        child.Value.Should().Be("App:Settings:MaxRetries");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateChild_NullOrWhitespace_ThrowsArgumentException(string? segment)
    {
        var parent = new ConfigKey("Database");

        var act = () => parent.CreateChild(segment!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromString_CreatesConfigKey()
    {
        var value = "Database:ConnectionString";

        var configKey = ConfigKey.FromString(value);

        configKey.Value.Should().Be(value);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var configKey = new ConfigKey("Database:ConnectionString");

        string value = configKey;

        value.Should().Be("Database:ConnectionString");
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesConfigKey()
    {
        ConfigKey configKey = "Database:ConnectionString";

        configKey.Value.Should().Be("Database:ConnectionString");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var key1 = new ConfigKey("Database:ConnectionString");
        var key2 = new ConfigKey("Database:ConnectionString");

        key1.Should().Be(key2);
        (key1 == key2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var key1 = new ConfigKey("Database:ConnectionString");
        var key2 = new ConfigKey("Database:Timeout");

        key1.Should().NotBe(key2);
        (key1 != key2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var configKey = new ConfigKey("Database:ConnectionString");

        var result = configKey.ToString();

        result.Should().Be("Database:ConnectionString");
    }

    [Fact]
    public void Value_DefaultStruct_ReturnsEmptyString()
    {
        var configKey = default(ConfigKey);

        configKey.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void GetSegments_WithEmptySegments_IgnoresEmptyParts()
    {
        var configKey = new ConfigKey("A::B");

        var segments = configKey.GetSegments();

        segments.Should().HaveCount(2);
        segments[0].Should().Be("A");
        segments[1].Should().Be("B");
    }
}
