using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Configuration;

namespace HoneyDrunk.Kernel.Tests.Configuration;

public class ConfigPathTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesConfigPath()
    {
        var scope = new TestConfigScope(ConfigScopeType.Studio, "test-studio");
        var key = new ConfigKey("Database:ConnectionString");

        var configPath = new ConfigPath(scope, key);

        configPath.Scope.Should().Be(scope);
        configPath.Key.Should().Be(key);
    }

    [Fact]
    public void Constructor_NullScope_ThrowsArgumentNullException()
    {
        var key = new ConfigKey("Database:ConnectionString");

        var act = () => new ConfigPath(null!, key);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FullPath_CombinesScopeAndKey()
    {
        var scope = new TestConfigScope(ConfigScopeType.Studio, "test-studio");
        var key = new ConfigKey("Database:ConnectionString");
        var configPath = new ConfigPath(scope, key);

        var fullPath = configPath.FullPath;

        fullPath.Should().Be("studio:test-studio/Database:ConnectionString");
    }

    [Fact]
    public void Parse_ValidPathString_ReturnsConfigPath()
    {
        var pathString = "studio:test-studio/Database:ConnectionString";
        static IConfigScope ScopeFactory(string scopePath)
        {
            return new TestConfigScope(ConfigScopeType.Studio, "test-studio", scopePath);
        }

        var configPath = ConfigPath.Parse(pathString, ScopeFactory);

        configPath.Scope.ScopePath.Should().Be("studio:test-studio");
        configPath.Key.Value.Should().Be("Database:ConnectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Parse_NullOrWhitespacePath_ThrowsArgumentException(string? pathString)
    {
        static IConfigScope ScopeFactory(string scopePath) => new TestConfigScope(ConfigScopeType.Global, null);

        var act = () => ConfigPath.Parse(pathString!, ScopeFactory);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_NullScopeFactory_ThrowsArgumentNullException()
    {
        var pathString = "studio:test-studio/Database:ConnectionString";

        var act = () => ConfigPath.Parse(pathString, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("no-slash")]
    [InlineData("studio:test-studio")]
    public void Parse_InvalidFormat_ThrowsArgumentException(string pathString)
    {
        static IConfigScope ScopeFactory(string scopePath) => new TestConfigScope(ConfigScopeType.Global, null);

        var act = () => ConfigPath.Parse(pathString, ScopeFactory);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid config path format*");
    }

    [Fact]
    public void ToString_ReturnsFullPath()
    {
        var scope = new TestConfigScope(ConfigScopeType.Node, "test-node");
        var key = new ConfigKey("App:Settings:MaxRetries");
        var configPath = new ConfigPath(scope, key);

        var result = configPath.ToString();

        result.Should().Be("node:test-node/App:Settings:MaxRetries");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var scope = new TestConfigScope(ConfigScopeType.Studio, "test-studio");
        var key = new ConfigKey("Database:ConnectionString");
        var path1 = new ConfigPath(scope, key);
        var path2 = new ConfigPath(scope, key);

        path1.Should().Be(path2);
    }

    [Fact]
    public void Equality_DifferentKeys_AreNotEqual()
    {
        var scope = new TestConfigScope(ConfigScopeType.Studio, "test-studio");
        var path1 = new ConfigPath(scope, new ConfigKey("Key1"));
        var path2 = new ConfigPath(scope, new ConfigKey("Key2"));

        path1.Should().NotBe(path2);
    }

    private sealed class TestConfigScope(ConfigScopeType scopeType, string? scopeId, string? scopePath = null) : IConfigScope
    {
        public ConfigScopeType ScopeType { get; } = scopeType;

        public string? ScopeId { get; } = scopeId;

        public IConfigScope? ParentScope { get; }

        public string ScopePath { get; } = scopePath ?? $"{scopeType.ToString().ToLowerInvariant()}:{scopeId}";

        public IConfigScope CreateChildScope(ConfigScopeType childScopeType, string childScopeId)
        {
            return new TestConfigScope(childScopeType, childScopeId);
        }
    }
}
