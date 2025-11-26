using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.Context;

public class GridContextAccessorTests
{
    [Fact]
    public void GridContext_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var accessor = new GridContextAccessor();

        // Act
        var context = accessor.GridContext;

        // Assert
        context.Should().BeNull();
    }

    [Fact]
    public void GridContext_WhenSet_ReturnsSetValue()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        accessor.GridContext = gridContext;

        // Assert
        accessor.GridContext.Should().BeSameAs(gridContext);
    }

    [Fact]
    public void GridContext_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
        accessor.GridContext = gridContext;

        // Act
        accessor.GridContext = null;

        // Assert
        accessor.GridContext.Should().BeNull();
    }

    [Fact]
    public async Task GridContext_IsIsolatedPerAsyncContext()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var context1 = new GridContext("corr1", Ulid.NewUlid().ToString(), "node1", "studio1", "env1");
        var context2 = new GridContext("corr2", Ulid.NewUlid().ToString(), "node2", "studio2", "env2");
        IGridContext? task1Result = null;
        IGridContext? task2Result = null;

        // Act
        var task1 = Task.Run(() =>
        {
            accessor.GridContext = context1;
            Thread.Sleep(50);
            task1Result = accessor.GridContext;
        });

        var task2 = Task.Run(() =>
        {
            accessor.GridContext = context2;
            Thread.Sleep(50);
            task2Result = accessor.GridContext;
        });

        await Task.WhenAll(task1, task2);

        // Assert
        task1Result.Should().BeSameAs(context1);
        task2Result.Should().BeSameAs(context2);
        task1Result.Should().NotBeSameAs(context2);
    }

    [Fact]
    public async Task GridContext_FlowsAcrossAsyncContinuations()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
        accessor.GridContext = context;

        // Act
        await Task.Delay(10);
        var afterDelay = accessor.GridContext;

        await Task.Run(async () =>
        {
            await Task.Delay(10);
        });

        var afterTaskRun = accessor.GridContext;

        // Assert
        afterDelay.Should().BeSameAs(context);
        afterTaskRun.Should().BeSameAs(context);
    }

    [Fact]
    public void GridContext_CanBeReplacedMultipleTimes()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var context1 = new GridContext("corr1", Ulid.NewUlid().ToString(), "node", "studio", "env");
        var context2 = new GridContext("corr2", Ulid.NewUlid().ToString(), "node", "studio", "env");
        var context3 = new GridContext("corr3", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act & Assert
        accessor.GridContext = context1;
        accessor.GridContext.Should().BeSameAs(context1);

        accessor.GridContext = context2;
        accessor.GridContext.Should().BeSameAs(context2);

        accessor.GridContext = context3;
        accessor.GridContext.Should().BeSameAs(context3);

        accessor.GridContext = null;
        accessor.GridContext.Should().BeNull();
    }

    [Fact]
    public void GridContext_PreservesContextProperties()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var baggage = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var context = new GridContext(
            correlationId: "test-correlation",
            operationId: Ulid.NewUlid().ToString(),
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "test-causation",
            baggage: baggage);

        // Act
        accessor.GridContext = context;
        var retrieved = accessor.GridContext;

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CorrelationId.Should().Be("test-correlation");
        retrieved.NodeId.Should().Be("test-node");
        retrieved.StudioId.Should().Be("test-studio");
        retrieved.Environment.Should().Be("test-env");
        retrieved.CausationId.Should().Be("test-causation");
        retrieved.Baggage.Should().HaveCount(2);
        retrieved.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        retrieved.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public async Task GridContext_IndependentAcrossSeparateAsyncTasks()
    {
        // Arrange
        var accessor = new GridContextAccessor();
        var context1 = new GridContext("corr1", Ulid.NewUlid().ToString(), "node", "studio", "env");
        var context2 = new GridContext("corr2", Ulid.NewUlid().ToString(), "node", "studio", "env");
        var context3 = new GridContext("corr3", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var task1 = Task.Run(async () =>
        {
            accessor.GridContext = context1;
            await Task.Delay(20);
            return accessor.GridContext;
        });

        var task2 = Task.Run(async () =>
        {
            accessor.GridContext = context2;
            await Task.Delay(20);
            return accessor.GridContext;
        });

        var task3 = Task.Run(async () =>
        {
            accessor.GridContext = context3;
            await Task.Delay(20);
            return accessor.GridContext;
        });

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results[0].Should().BeSameAs(context1);
        results[1].Should().BeSameAs(context2);
        results[2].Should().BeSameAs(context3);
    }
}
