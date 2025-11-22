using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Configuration;

namespace HoneyDrunk.Kernel.Tests.Configuration;

public class ConfigScopeTypeTests
{
    [Theory]
    [InlineData(ConfigScopeType.Global, 0)]
    [InlineData(ConfigScopeType.Studio, 1)]
    [InlineData(ConfigScopeType.Node, 2)]
    [InlineData(ConfigScopeType.Tenant, 3)]
    [InlineData(ConfigScopeType.Project, 4)]
    [InlineData(ConfigScopeType.Request, 5)]
    public void EnumValues_HaveExpectedValues(ConfigScopeType scopeType, int expectedValue)
    {
        ((int)scopeType).Should().Be(expectedValue);
    }

    [Fact]
    public void EnumValues_AreOrdered()
    {
        var global = (int)ConfigScopeType.Global;
        var studio = (int)ConfigScopeType.Studio;
        var node = (int)ConfigScopeType.Node;
        var tenant = (int)ConfigScopeType.Tenant;
        var project = (int)ConfigScopeType.Project;
        var request = (int)ConfigScopeType.Request;

        global.Should().BeLessThan(studio);
        studio.Should().BeLessThan(node);
        node.Should().BeLessThan(tenant);
        tenant.Should().BeLessThan(project);
        project.Should().BeLessThan(request);
    }

    [Fact]
    public void AllEnumValues_CanBeEnumerated()
    {
        var values = Enum.GetValues<ConfigScopeType>();

        values.Should().HaveCount(6);
        values.Should().Contain(ConfigScopeType.Global);
        values.Should().Contain(ConfigScopeType.Studio);
        values.Should().Contain(ConfigScopeType.Node);
        values.Should().Contain(ConfigScopeType.Tenant);
        values.Should().Contain(ConfigScopeType.Project);
        values.Should().Contain(ConfigScopeType.Request);
    }
}
