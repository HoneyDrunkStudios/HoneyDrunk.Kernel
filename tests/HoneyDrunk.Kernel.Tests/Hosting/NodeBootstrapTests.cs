using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Transport;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Kernel.Tests.Hosting;

public sealed class NodeBootstrapTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("2.3.4")]
    public void BootstrapPreservesIdentityAndScopedContext(string? version)
    {
        var services = CreateServices(version);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var node = provider.GetRequiredService<INodeContext>();
        var descriptor = provider.GetRequiredService<INodeDescriptor>();
        var options = provider.GetRequiredService<IOptions<HoneyDrunkNodeOptions>>().Value;

        Assert.Equal("bootstrap-test", node.NodeId);
        Assert.Equal("test-studio", node.StudioId);
        Assert.Equal("development", node.Environment);
        Assert.Equal(version ?? typeof(HoneyDrunkNodeServiceCollectionExtensions).Assembly.GetName().Version!.ToString(), node.Version);
        Assert.Equal(node.NodeId, descriptor.NodeId);
        Assert.Equal(node.Version, descriptor.Version);
        Assert.Equal("core", descriptor.Sector);
        Assert.Equal("integration", descriptor.Tags["purpose"]);
        Assert.Equal(node.NodeId, descriptor.Name);
        Assert.Equal(node.NodeId, descriptor.Description);
        Assert.Empty(descriptor.Capabilities);
        Assert.Empty(descriptor.Dependencies);
        Assert.Empty(descriptor.Slots);
        Assert.Null(descriptor.Manifest);
        Assert.Null(descriptor.Cluster);
        Assert.False(descriptor.HasCapability("unregistered"));
        Assert.Same(options, provider.GetRequiredService<HoneyDrunkNodeOptions>());
        Assert.Equal(3, provider.GetServices<ITransportEnvelopeBinder>().Count());

        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var grid = first.ServiceProvider.GetRequiredService<GridContext>();
        Assert.Same(grid, first.ServiceProvider.GetRequiredService<IGridContext>());
        Assert.NotSame(grid, second.ServiceProvider.GetRequiredService<IGridContext>());
    }

    [Fact]
    public void OperationsRequireInitializationAndPropagateCausationToChildContexts()
    {
        using var provider = CreateServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var grid = scope.ServiceProvider.GetRequiredService<GridContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IOperationContextFactory>();
        Assert.Throws<InvalidOperationException>(() => factory.Create("too-early"));

        grid.Initialize(
            Ulid.NewUlid().ToString(),
            tenantId: new TenantId(Ulid.NewUlid().ToString()),
            projectId: "project-a",
            baggage: new Dictionary<string, string> { ["source"] = "parent", ["preserved"] = "yes" });
        using var operation = factory.Create("dispatch", new Dictionary<string, object?> { ["attempt"] = 1 });
        Assert.Same(operation, provider.GetRequiredService<IOperationContextAccessor>().Current);

        var child = provider.GetRequiredService<IGridContextFactory>().CreateChild(
            grid,
            operation,
            "downstream",
            new Dictionary<string, string> { ["source"] = "child" });
        Assert.Equal(grid.CorrelationId, child.CorrelationId);
        Assert.Equal(operation.OperationId, child.CausationId);
        Assert.Equal("downstream", child.NodeId);
        Assert.Equal(grid.TenantId, child.TenantId);
        Assert.Equal(grid.ProjectId, child.ProjectId);
        Assert.Equal("child", child.Baggage["source"]);
        Assert.Equal("yes", child.Baggage["preserved"]);
        Assert.Equal("parent", grid.Baggage["source"]);

        var accessor = provider.GetRequiredService<ScopedGridContextAccessor>();
        using (ScopedGridContextAccessor.SetContext(grid))
        {
            Assert.Same(grid, accessor.GridContext);
        }

        Assert.Throws<InvalidOperationException>(() => accessor.GridContext);
    }

    [Fact]
    public void DuplicateRegistrationIsRejected()
    {
        var services = CreateServices();
        Assert.Throws<InvalidOperationException>(() => services.AddHoneyDrunkNode(_ => { }));
    }

    [Theory]
    [InlineData("NodeId")]
    [InlineData("SectorId")]
    [InlineData("EnvironmentId")]
    [InlineData("StudioId")]
    public void MissingIdentityFailsBeforeTheNodeCanRun(string missing)
    {
        var options = new HoneyDrunkNodeOptions
        {
            NodeId = missing == "NodeId" ? null : new NodeId("bootstrap-test"),
            SectorId = missing == "SectorId" ? null : new SectorId("core"),
            EnvironmentId = missing == "EnvironmentId" ? null : new EnvironmentId("development"),
            StudioId = missing == "StudioId" ? " " : "test-studio",
        };
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains(missing, exception.Message, StringComparison.Ordinal);
    }

    private static ServiceCollection CreateServices(string? version = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = new NodeId("bootstrap-test");
            options.SectorId = new SectorId("core");
            options.EnvironmentId = new EnvironmentId("development");
            options.StudioId = "test-studio";
            options.Version = version;
            options.Tags["purpose"] = "integration";
        });
        Assert.Same(services, builder.Services);
        return services;
    }
}
