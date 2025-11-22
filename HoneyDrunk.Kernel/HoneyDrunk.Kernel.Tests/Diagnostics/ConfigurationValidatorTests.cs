using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Kernel.Tests.Diagnostics;

public class ConfigurationValidatorTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationValidator(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateNodeDescriptor_ValidDescriptor_ReturnsTrue()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateNodeDescriptor_MissingNodeId_ReturnsFalse()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        descriptor.NodeId = string.Empty;

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("NodeId is required");
    }

    [Fact]
    public void ValidateNodeDescriptor_MissingVersion_ReturnsFalse()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        descriptor.Version = string.Empty;

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("Version is required");
    }

    [Fact]
    public void ValidateNodeDescriptor_MissingName_ReturnsFalse()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        descriptor.Name = string.Empty;

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("Name is required");
    }

    [Fact]
    public void ValidateNodeDescriptor_EmptyCapabilityName_ReturnsFalse()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        var capability = new TestNodeCapability { Name = string.Empty };
        descriptor.Capabilities = [capability];

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("Capability name cannot be empty");
    }

    [Fact]
    public void ValidateNodeDescriptor_DuplicateCapabilityNames_ReturnsFalse()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        var capability1 = new TestNodeCapability { Name = "duplicated" };
        var capability2 = new TestNodeCapability { Name = "duplicated" };
        descriptor.Capabilities = [capability1, capability2];

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("Duplicate capability name: duplicated");
    }

    [Fact]
    public void ValidateNodeDescriptor_EmptyDependency_ReturnsFalse()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        descriptor.Dependencies = ["valid-dependency", string.Empty];

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("Dependency cannot be empty");
    }

    [Fact]
    public void ValidateNodeDescriptor_MultipleErrors_ReturnsAllErrors()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        descriptor.NodeId = string.Empty;
        descriptor.Version = string.Empty;
        descriptor.Name = string.Empty;

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeFalse();
        errors.Should().HaveCount(3);
        errors.Should().Contain("NodeId is required");
        errors.Should().Contain("Version is required");
        errors.Should().Contain("Name is required");
    }

    [Fact]
    public void ValidateNodeDescriptor_NullDependencies_DoesNotThrow()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        descriptor.Dependencies = [];

        var act = () => validator.ValidateNodeDescriptor(descriptor, out _);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateNodeDescriptor_ValidCapabilities_ReturnsTrue()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var descriptor = CreateValidNodeDescriptor();
        var capability1 = new TestNodeCapability { Name = "capability1" };
        var capability2 = new TestNodeCapability { Name = "capability2" };
        descriptor.Capabilities = [capability1, capability2];

        var result = validator.ValidateNodeDescriptor(descriptor, out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateStudioConfiguration_ValidConfiguration_ReturnsTrue()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var config = new TestStudioConfiguration
        {
            StudioId = "test-studio",
            VaultEndpoint = "https://vault.example.com",
            ObservabilityEndpoint = "https://observability.example.com"
        };

        var result = validator.ValidateStudioConfiguration(config, out var warnings);

        result.Should().BeTrue();
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void ValidateStudioConfiguration_MissingVaultEndpoint_ReturnsWarning()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var config = new TestStudioConfiguration
        {
            StudioId = "test-studio",
            VaultEndpoint = null,
            ObservabilityEndpoint = "https://observability.example.com"
        };

        var result = validator.ValidateStudioConfiguration(config, out var warnings);

        result.Should().BeTrue();
        warnings.Should().Contain("VaultEndpoint is not configured - secrets management may be limited");
    }

    [Fact]
    public void ValidateStudioConfiguration_MissingObservabilityEndpoint_ReturnsWarning()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var config = new TestStudioConfiguration
        {
            StudioId = "test-studio",
            VaultEndpoint = "https://vault.example.com",
            ObservabilityEndpoint = null
        };

        var result = validator.ValidateStudioConfiguration(config, out var warnings);

        result.Should().BeTrue();
        warnings.Should().Contain("ObservabilityEndpoint is not configured - telemetry may be degraded");
    }

    [Fact]
    public void ValidateStudioConfiguration_MultipleWarnings_ReturnsAllWarnings()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var config = new TestStudioConfiguration
        {
            StudioId = "test-studio",
            VaultEndpoint = null,
            ObservabilityEndpoint = null
        };

        var result = validator.ValidateStudioConfiguration(config, out var warnings);

        result.Should().BeTrue();
        warnings.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateStudioConfiguration_EmptyVaultEndpoint_ReturnsWarning()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var config = new TestStudioConfiguration
        {
            StudioId = "test-studio",
            VaultEndpoint = string.Empty,
            ObservabilityEndpoint = "https://observability.example.com"
        };

        var result = validator.ValidateStudioConfiguration(config, out var warnings);

        result.Should().BeTrue();
        warnings.Should().Contain("VaultEndpoint is not configured - secrets management may be limited");
    }

    [Fact]
    public void ValidateStudioConfiguration_WhitespaceObservabilityEndpoint_ReturnsWarning()
    {
        var validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
        var config = new TestStudioConfiguration
        {
            StudioId = "test-studio",
            VaultEndpoint = "https://vault.example.com",
            ObservabilityEndpoint = "   "
        };

        var result = validator.ValidateStudioConfiguration(config, out var warnings);

        result.Should().BeTrue();
        warnings.Should().Contain("ObservabilityEndpoint is not configured - telemetry may be degraded");
    }

    private static TestNodeDescriptor CreateValidNodeDescriptor()
    {
        return new TestNodeDescriptor
        {
            NodeId = "test-node",
            Version = "1.0.0",
            Name = "Test Node",
            Description = "Test Description",
            Capabilities = [],
            Dependencies = []
        };
    }

    private sealed class TestNodeDescriptor : INodeDescriptor
    {
        public required string NodeId { get; set; }

        public required string Version { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public string? Sector { get; set; }

        public string? Cluster { get; set; }

        public required IReadOnlyList<INodeCapability> Capabilities { get; set; }

        public IReadOnlyList<string> Dependencies { get; set; } = [];

        public IReadOnlyList<string> Slots { get; } = [];

        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        public INodeManifest? Manifest { get; }

        public bool HasCapability(string capabilityName) => false;
    }

    private sealed class TestNodeCapability : INodeCapability
    {
        public required string Name { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public IReadOnlyList<string> SupportedProtocols { get; set; } = [];

        public IReadOnlyDictionary<string, string> Endpoints { get; set; } = new Dictionary<string, string>();

        public string? InputSchema { get; set; }

        public string? OutputSchema { get; set; }

        public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    private sealed class TestStudioConfiguration : IStudioConfiguration
    {
        public required string StudioId { get; init; }

        public string Environment { get; } = "test";

        public string? VaultEndpoint { get; init; }

        public string? ObservabilityEndpoint { get; init; }

        public string? ServiceDiscoveryEndpoint { get; init; }

        public IReadOnlyDictionary<string, bool> FeatureFlags { get; } = new Dictionary<string, bool>();

        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        public bool TryGetValue(string key, out string? value)
        {
            value = null;
            return false;
        }
    }
}
