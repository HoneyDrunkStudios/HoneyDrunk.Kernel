using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Health;

namespace HoneyDrunk.Kernel.Tests.Health;

public class HealthStatusTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, 0)]
    [InlineData(HealthStatus.Degraded, 1)]
    [InlineData(HealthStatus.Unhealthy, 2)]
    public void EnumValues_HaveExpectedValues(HealthStatus status, int expectedValue)
    {
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void AllEnumValues_CanBeEnumerated()
    {
        var values = Enum.GetValues<HealthStatus>();

        values.Should().HaveCount(3);
        values.Should().Contain(HealthStatus.Healthy);
        values.Should().Contain(HealthStatus.Degraded);
        values.Should().Contain(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Comparison_HealthyIsLessThanDegraded()
    {
        var healthy = (int)HealthStatus.Healthy;
        var degraded = (int)HealthStatus.Degraded;

        healthy.Should().BeLessThan(degraded);
    }

    [Fact]
    public void Comparison_DegradedIsLessThanUnhealthy()
    {
        var degraded = (int)HealthStatus.Degraded;
        var unhealthy = (int)HealthStatus.Unhealthy;

        degraded.Should().BeLessThan(unhealthy);
    }

    [Fact]
    public void ToString_ReturnsEnumName()
    {
        HealthStatus.Healthy.ToString().Should().Be("Healthy");
        HealthStatus.Degraded.ToString().Should().Be("Degraded");
        HealthStatus.Unhealthy.ToString().Should().Be("Unhealthy");
    }
}
