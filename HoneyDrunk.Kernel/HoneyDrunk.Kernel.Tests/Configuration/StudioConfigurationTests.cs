using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Configuration;
using HoneyDrunk.Kernel.Configuration;
using Microsoft.Extensions.Configuration;

namespace HoneyDrunk.Kernel.Tests.Configuration;

public class StudioConfigurationTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesConfiguration()
    {
        var configuration = CreateTestConfiguration();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.StudioId.Should().Be("test-studio");
        studioConfig.Environment.Should().Be("production");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespaceStudioId_ThrowsArgumentException(string? studioId)
    {
        var configuration = CreateTestConfiguration();

        var act = () => new StudioConfiguration(studioId!, "production", configuration);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespaceEnvironment_ThrowsArgumentException(string? environment)
    {
        var configuration = CreateTestConfiguration();

        var act = () => new StudioConfiguration("test-studio", environment!, configuration);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        var act = () => new StudioConfiguration("test-studio", "production", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_LoadsVaultEndpoint()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:VaultEndpoint"] = "https://vault.example.com"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.VaultEndpoint.Should().Be("https://vault.example.com");
    }

    [Fact]
    public void Constructor_LoadsObservabilityEndpoint()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:ObservabilityEndpoint"] = "https://observability.example.com"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.ObservabilityEndpoint.Should().Be("https://observability.example.com");
    }

    [Fact]
    public void Constructor_LoadsServiceDiscoveryEndpoint()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:ServiceDiscoveryEndpoint"] = "https://discovery.example.com"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.ServiceDiscoveryEndpoint.Should().Be("https://discovery.example.com");
    }

    [Fact]
    public void Constructor_LoadsFeatureFlags()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:FeatureFlags:EnableNewFeature"] = "true",
            ["Studio:FeatureFlags:EnableBetaAccess"] = "false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.FeatureFlags.Should().HaveCount(2);
        studioConfig.FeatureFlags["EnableNewFeature"].Should().BeTrue();
        studioConfig.FeatureFlags["EnableBetaAccess"].Should().BeFalse();
    }

    [Fact]
    public void Constructor_LoadsTags()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:Tags:team"] = "platform",
            ["Studio:Tags:region"] = "us-west"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.Tags.Should().HaveCount(2);
        studioConfig.Tags["team"].Should().Be("platform");
        studioConfig.Tags["region"].Should().Be("us-west");
    }

    [Fact]
    public void Constructor_EmptyConfiguration_CreatesEmptyCollections()
    {
        var configuration = new ConfigurationBuilder().Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.FeatureFlags.Should().BeEmpty();
        studioConfig.Tags.Should().BeEmpty();
        studioConfig.VaultEndpoint.Should().BeNull();
        studioConfig.ObservabilityEndpoint.Should().BeNull();
        studioConfig.ServiceDiscoveryEndpoint.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void TryGetValue_NullOrWhitespaceKey_ThrowsArgumentException(string? key)
    {
        var configuration = CreateTestConfiguration();
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        var act = () => studioConfig.TryGetValue(key!, out _);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetValue_KeyExistsInConfiguration_ReturnsTrue()
    {
        var configData = new Dictionary<string, string?>
        {
            ["TestKey"] = "TestValue"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        var result = studioConfig.TryGetValue("TestKey", out var value);

        result.Should().BeTrue();
        value.Should().Be("TestValue");
    }

    [Fact]
    public void TryGetValue_KeyDoesNotExist_ReturnsFalse()
    {
        var configuration = CreateTestConfiguration();
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        var result = studioConfig.TryGetValue("NonExistentKey", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_WithSecretsSource_TriesConfigurationFirst()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Key"] = "ConfigValue"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var secretsSource = new TestSecretsSource
        {
            ["Key"] = "SecretValue"
        };
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration, secretsSource);

        var result = studioConfig.TryGetValue("Key", out var value);

        result.Should().BeTrue();
        value.Should().Be("ConfigValue");
    }

    [Fact]
    public void TryGetValue_NotInConfigurationButInSecrets_ReturnsSecretValue()
    {
        var configuration = CreateTestConfiguration();
        var secretsSource = new TestSecretsSource
        {
            ["SecretKey"] = "SecretValue"
        };
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration, secretsSource);

        var result = studioConfig.TryGetValue("SecretKey", out var value);

        result.Should().BeTrue();
        value.Should().Be("SecretValue");
    }

    [Fact]
    public void TryGetValue_WithoutSecretsSource_OnlyChecksConfiguration()
    {
        var configuration = CreateTestConfiguration();
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        var result = studioConfig.TryGetValue("AnyKey", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void FeatureFlags_IsReadOnly()
    {
        var configuration = CreateTestConfiguration();
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.FeatureFlags.Should().BeAssignableTo<IReadOnlyDictionary<string, bool>>();
    }

    [Fact]
    public void Tags_IsReadOnly()
    {
        var configuration = CreateTestConfiguration();
        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.Tags.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
    }

    [Fact]
    public void Constructor_InvalidFeatureFlagValue_SkipsFlag()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:FeatureFlags:ValidFlag"] = "true",
            ["Studio:FeatureFlags:InvalidFlag"] = "not-a-boolean"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.FeatureFlags.Should().HaveCount(1);
        studioConfig.FeatureFlags.Should().ContainKey("ValidFlag");
        studioConfig.FeatureFlags.Should().NotContainKey("InvalidFlag");
    }

    [Fact]
    public void Constructor_NullTagValue_SkipsTag()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Studio:Tags:ValidTag"] = "value",
            ["Studio:Tags:NullTag"] = null
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var studioConfig = new StudioConfiguration("test-studio", "production", configuration);

        studioConfig.Tags.Should().HaveCount(1);
        studioConfig.Tags.Should().ContainKey("ValidTag");
        studioConfig.Tags.Should().NotContainKey("NullTag");
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private sealed class TestSecretsSource : Dictionary<string, string>, ISecretsSource
    {
        public bool TryGetSecret(string key, out string? value)
        {
            return TryGetValue(key, out value);
        }
    }
}
