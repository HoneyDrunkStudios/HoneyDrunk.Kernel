using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class EnvironmentIdTests
{
    [Theory]
    [InlineData("production")]
    [InlineData("dev-alice")]
    [InlineData("perf-test")]
    public void Constructor_WithValidValue_CreatesEnvironmentId(string value)
    {
        var id = new EnvironmentId(value);

        id.Value.Should().Be(value);
        id.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("Production")]
    [InlineData("dev--alice")]
    [InlineData("dev_")]
    public void Constructor_WithInvalidValue_ThrowsArgumentException(string? value)
    {
        var act = () => new EnvironmentId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("staging", true)]
    [InlineData("local-dev", true)]
    [InlineData("Staging", false)]
    [InlineData("ab", false)]
    public void IsValid_ReturnsExpectedResult(string value, bool expected)
    {
        var result = EnvironmentId.IsValid(value, out var errorMessage);

        result.Should().Be(expected);
        (errorMessage is null).Should().Be(expected);
    }

    [Fact]
    public void TryParse_WithValidValue_ReturnsTrue()
    {
        var result = EnvironmentId.TryParse("integration", out var id);

        result.Should().BeTrue();
        id.Value.Should().Be("integration");
    }

    [Fact]
    public void TryParse_WithInvalidValue_ReturnsFalse()
    {
        var result = EnvironmentId.TryParse("Integration", out var id);

        result.Should().BeFalse();
        id.Should().Be(default(EnvironmentId));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var id = new EnvironmentId("testing");

        string value = id;

        value.Should().Be("testing");
    }

    [Fact]
    public void WellKnown_ValuesAreStable()
    {
        EnvironmentId.WellKnown.Production.Value.Should().Be("production");
        EnvironmentId.WellKnown.Staging.Value.Should().Be("staging");
        EnvironmentId.WellKnown.Development.Value.Should().Be("development");
        EnvironmentId.WellKnown.Testing.Value.Should().Be("testing");
        EnvironmentId.WellKnown.Performance.Value.Should().Be("performance");
        EnvironmentId.WellKnown.Integration.Value.Should().Be("integration");
        EnvironmentId.WellKnown.Local.Value.Should().Be("local");
    }
}
