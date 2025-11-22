using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Configuration;

namespace HoneyDrunk.Kernel.Tests.Configuration;

public class NodeRuntimeOptionsTests
{
    [Fact]
    public void Constructor_DefaultValues_AreSet()
    {
        var options = new NodeRuntimeOptions();

        options.Environment.Should().Be("production");
        options.Region.Should().BeNull();
        options.DeploymentRing.Should().BeNull();
        options.EnableDetailedTelemetry.Should().BeTrue();
        options.EnableDistributedTracing.Should().BeTrue();
        options.TelemetrySamplingRate.Should().Be(1.0);
        options.HealthCheckIntervalSeconds.Should().Be(30);
        options.ShutdownGracePeriodSeconds.Should().Be(30);
        options.EnableSecretRotation.Should().BeTrue();
        options.SecretRotationIntervalMinutes.Should().Be(60);
        options.Tags.Should().NotBeNull();
        options.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Environment_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            Environment = "development"
        };

        options.Environment.Should().Be("development");
    }

    [Fact]
    public void Region_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            Region = "us-east-1"
        };

        options.Region.Should().Be("us-east-1");
    }

    [Fact]
    public void DeploymentRing_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            DeploymentRing = "canary"
        };

        options.DeploymentRing.Should().Be("canary");
    }

    [Fact]
    public void EnableDetailedTelemetry_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            EnableDetailedTelemetry = false
        };

        options.EnableDetailedTelemetry.Should().BeFalse();
    }

    [Fact]
    public void EnableDistributedTracing_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            EnableDistributedTracing = false
        };

        options.EnableDistributedTracing.Should().BeFalse();
    }

    [Fact]
    public void TelemetrySamplingRate_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            TelemetrySamplingRate = 0.5
        };

        options.TelemetrySamplingRate.Should().Be(0.5);
    }

    [Fact]
    public void HealthCheckIntervalSeconds_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            HealthCheckIntervalSeconds = 60
        };

        options.HealthCheckIntervalSeconds.Should().Be(60);
    }

    [Fact]
    public void ShutdownGracePeriodSeconds_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            ShutdownGracePeriodSeconds = 60
        };

        options.ShutdownGracePeriodSeconds.Should().Be(60);
    }

    [Fact]
    public void EnableSecretRotation_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            EnableSecretRotation = false
        };

        options.EnableSecretRotation.Should().BeFalse();
    }

    [Fact]
    public void SecretRotationIntervalMinutes_CanBeSet()
    {
        var options = new NodeRuntimeOptions
        {
            SecretRotationIntervalMinutes = 120
        };

        options.SecretRotationIntervalMinutes.Should().Be(120);
    }

    [Fact]
    public void Tags_CanBeSet()
    {
        var tags = new Dictionary<string, string>
        {
            ["version"] = "1.0.0",
            ["team"] = "platform"
        };

        var options = new NodeRuntimeOptions
        {
            Tags = tags
        };

        options.Tags.Should().HaveCount(2);
        options.Tags["version"].Should().Be("1.0.0");
        options.Tags["team"].Should().Be("platform");
    }

    [Fact]
    public void Record_Equality_WorksCorrectly()
    {
        var sharedTags = new Dictionary<string, string>();

        var options1 = new NodeRuntimeOptions
        {
            Environment = "staging",
            Region = "us-west-2",
            Tags = sharedTags
        };

        var options2 = new NodeRuntimeOptions
        {
            Environment = "staging",
            Region = "us-west-2",
            Tags = sharedTags
        };

        options1.Should().Be(options2);
    }

    [Fact]
    public void Record_Inequality_WorksCorrectly()
    {
        var options1 = new NodeRuntimeOptions
        {
            Environment = "staging"
        };

        var options2 = new NodeRuntimeOptions
        {
            Environment = "production"
        };

        options1.Should().NotBe(options2);
    }

    [Fact]
    public void With_CreatesNewInstance()
    {
        var original = new NodeRuntimeOptions
        {
            Environment = "production"
        };

        var modified = original with { Environment = "staging" };

        modified.Environment.Should().Be("staging");
        original.Environment.Should().Be("production");
    }

    [Fact]
    public void AllProperties_CanBeSetViaInitializer()
    {
        var options = new NodeRuntimeOptions
        {
            Environment = "staging",
            Region = "eu-west-1",
            DeploymentRing = "ring-1",
            EnableDetailedTelemetry = false,
            EnableDistributedTracing = false,
            TelemetrySamplingRate = 0.25,
            HealthCheckIntervalSeconds = 45,
            ShutdownGracePeriodSeconds = 45,
            EnableSecretRotation = false,
            SecretRotationIntervalMinutes = 90,
            Tags = new Dictionary<string, string> { ["key"] = "value" }
        };

        options.Environment.Should().Be("staging");
        options.Region.Should().Be("eu-west-1");
        options.DeploymentRing.Should().Be("ring-1");
        options.EnableDetailedTelemetry.Should().BeFalse();
        options.EnableDistributedTracing.Should().BeFalse();
        options.TelemetrySamplingRate.Should().Be(0.25);
        options.HealthCheckIntervalSeconds.Should().Be(45);
        options.ShutdownGracePeriodSeconds.Should().Be(45);
        options.EnableSecretRotation.Should().BeFalse();
        options.SecretRotationIntervalMinutes.Should().Be(90);
        options.Tags["key"].Should().Be("value");
    }
}
