using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class SectorIdTests
{
    [Theory]
    [InlineData("core")]
    [InlineData("ai")]
    [InlineData("honey-play")]
    public void Constructor_WithValidValue_CreatesSectorId(string value)
    {
        var id = new SectorId(value);

        id.Value.Should().Be(value);
        id.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Core")]
    [InlineData("honey_play")]
    [InlineData("honey--play")]
    public void Constructor_WithInvalidValue_ThrowsArgumentException(string? value)
    {
        var act = () => new SectorId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("core", true)]
    [InlineData("ai", true)]
    [InlineData("AI", false)]
    [InlineData("a", false)]
    public void IsValid_ReturnsExpectedResult(string value, bool expected)
    {
        var result = SectorId.IsValid(value, out var errorMessage);

        result.Should().Be(expected);
        (errorMessage is null).Should().Be(expected);
    }

    [Fact]
    public void TryParse_WithValidValue_ReturnsTrue()
    {
        var result = SectorId.TryParse("ops", out var id);

        result.Should().BeTrue();
        id.Value.Should().Be("ops");
    }

    [Fact]
    public void TryParse_WithInvalidValue_ReturnsFalse()
    {
        var result = SectorId.TryParse("Ops", out var id);

        result.Should().BeFalse();
        id.Should().Be(default(SectorId));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var id = new SectorId("meta");

        string value = id;

        value.Should().Be("meta");
    }

    [Fact]
    public void WellKnown_ValuesAreStable()
    {
        SectorId.WellKnown.Core.Value.Should().Be("core");
        SectorId.WellKnown.Ops.Value.Should().Be("ops");
        SectorId.WellKnown.AI.Value.Should().Be("ai");
        SectorId.WellKnown.Creator.Value.Should().Be("creator");
        SectorId.WellKnown.Market.Value.Should().Be("market");
        SectorId.WellKnown.HoneyPlay.Value.Should().Be("honeyplay");
        SectorId.WellKnown.Cyberware.Value.Should().Be("cyberware");
        SectorId.WellKnown.HoneyNet.Value.Should().Be("honeynet");
        SectorId.WellKnown.Meta.Value.Should().Be("meta");
    }
}
