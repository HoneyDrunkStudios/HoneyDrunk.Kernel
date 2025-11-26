using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Context.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Kernel.Tests.Context.Middleware;

public class GridContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidRequest_CreatesGridContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "test-corr-123";
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var opFactory = CreateTestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, opFactory);

        httpContext.Response.Headers.Should().ContainKey(GridHeaderNames.CorrelationId);
        httpContext.Response.Headers[GridHeaderNames.CorrelationId].ToString().Should().Be("test-corr-123");
        httpContext.Response.Headers.Should().ContainKey(GridHeaderNames.NodeId);
        httpContext.Response.Headers[GridHeaderNames.NodeId].ToString().Should().Be("test-node");
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationHeader_GeneratesNewCorrelation()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/orders";

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var opFactory = CreateTestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, opFactory);

        httpContext.Response.Headers.Should().ContainKey(GridHeaderNames.CorrelationId);
        httpContext.Response.Headers[GridHeaderNames.CorrelationId].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_CreatesOperationContextWithMetadata()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "PUT";
        httpContext.Request.Path = "/api/users/123";
        httpContext.TraceIdentifier = "trace-456";

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var testOpFactory = new TestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, testOpFactory);

        testOpFactory.LastCreatedOperationName.Should().Be("HttpRequest");
        testOpFactory.LastCreatedMetadata.Should().ContainKey("http.method");
        testOpFactory.LastCreatedMetadata!["http.method"].Should().Be("PUT");
        testOpFactory.LastCreatedMetadata.Should().ContainKey("http.path");
        testOpFactory.LastCreatedMetadata["http.path"].Should().Be("/api/users/123");
        testOpFactory.LastCreatedMetadata.Should().ContainKey("http.request_id");
        testOpFactory.LastCreatedMetadata["http.request_id"].Should().Be("trace-456");
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_FailsOperationAndRethrows()
    {
        var expectedException = new InvalidOperationException("Test error");
        var httpContext = new DefaultHttpContext();

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var testOpFactory = new TestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: _ => throw expectedException,
            logger: NullLogger<GridContextMiddleware>.Instance);

        var act = async () => await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, testOpFactory);

        await act.Should().ThrowAsync<InvalidOperationException>();
        testOpFactory.LastCreatedOperation.Should().NotBeNull();
        testOpFactory.LastCreatedOperation!.IsSuccess.Should().BeFalse();
        testOpFactory.LastCreatedOperation.ErrorMessage.Should().Be("Unhandled exception");
    }

    [Fact]
    public async Task InvokeAsync_ClearsAmbientContextsAfterRequest()
    {
        var httpContext = new DefaultHttpContext();

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var opFactory = CreateTestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, opFactory);

        gridAccessor.GridContext.Should().BeNull();
        opAccessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithVeryLongHeaders_TruncatesToMaxLength()
    {
        var httpContext = new DefaultHttpContext();
        var longValue = new string('x', 300);
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = longValue;

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var capturedContext = (IGridContext?)null;
        var opFactory = new TestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedContext = gridAccessor.GridContext;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, opFactory);

        capturedContext.Should().NotBeNull();
        capturedContext!.CorrelationId.Length.Should().BeLessThanOrEqualTo(256);
    }

    [Fact]
    public async Task InvokeAsync_WithCausationHeader_PropagatesCausationId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "corr-123";
        httpContext.Request.Headers[GridHeaderNames.CausationId] = "cause-456";

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var capturedContext = (IGridContext?)null;
        var opFactory = new TestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedContext = gridAccessor.GridContext;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, opFactory);

        capturedContext.Should().NotBeNull();
        capturedContext!.CorrelationId.Should().Be("corr-123");
        capturedContext.CausationId.Should().Be("cause-456");
    }

    [Fact]
    public async Task InvokeAsync_SuccessfulRequest_CompletesOperation()
    {
        var httpContext = new DefaultHttpContext();

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var testOpFactory = new TestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, testOpFactory);

        testOpFactory.LastCreatedOperation.Should().NotBeNull();
        testOpFactory.LastCreatedOperation!.IsSuccess.Should().BeTrue();
        testOpFactory.LastCreatedOperation.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithBaggageHeaders_PropagatesBaggage()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[$"{GridHeaderNames.BaggagePrefix}tenant-id"] = "tenant-123";
        httpContext.Request.Headers[$"{GridHeaderNames.BaggagePrefix}user-id"] = "user-456";

        var nodeContext = CreateTestNodeContext();
        var gridAccessor = new GridContextAccessor();
        var opAccessor = new OperationContextAccessor();
        var capturedContext = (IGridContext?)null;
        var opFactory = new TestOperationContextFactory();

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedContext = gridAccessor.GridContext;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, nodeContext, gridAccessor, opAccessor, opFactory);

        capturedContext.Should().NotBeNull();
        capturedContext!.Baggage.Should().ContainKey("tenant-id").WhoseValue.Should().Be("tenant-123");
        capturedContext.Baggage.Should().ContainKey("user-id").WhoseValue.Should().Be("user-456");
    }

    private static TestNodeContext CreateTestNodeContext()
    {
        return new TestNodeContext
        {
            NodeId = "test-node",
            Version = "1.0.0",
            StudioId = "test-studio",
            Environment = "test",
        };
    }

    private static TestOperationContextFactory CreateTestOperationContextFactory() => new();

    private sealed class TestNodeContext : INodeContext
    {
        public required string NodeId { get; init; }

        public required string Version { get; init; }

        public required string StudioId { get; init; }

        public required string Environment { get; init; }

        public NodeLifecycleStage LifecycleStage { get; private set; } = NodeLifecycleStage.Initializing;

        public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

        public string MachineName { get; } = System.Environment.MachineName;

        public int ProcessId { get; } = System.Environment.ProcessId;

        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        public void SetLifecycleStage(NodeLifecycleStage stage)
        {
            LifecycleStage = stage;
        }
    }

    private sealed class TestOperationContextFactory : IOperationContextFactory
    {
        public string? LastCreatedOperationName { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastCreatedMetadata { get; private set; }

        public TestOperationContext? LastCreatedOperation { get; private set; }

        public IOperationContext Create(string operationName, IReadOnlyDictionary<string, object?>? metadata = null)
        {
            LastCreatedOperationName = operationName;
            LastCreatedMetadata = metadata;
            var gridContext = new GridContext("test-corr", "test-op", "test-node", "test-studio", "test-env");
            LastCreatedOperation = new TestOperationContext(gridContext, operationName, metadata);
            return LastCreatedOperation;
        }
    }

    private sealed class TestOperationContext(IGridContext gridContext, string operationName, IReadOnlyDictionary<string, object?>? metadata) : IOperationContext
    {
        private readonly Dictionary<string, object?> _metadata = metadata != null ? new Dictionary<string, object?>(metadata) : [];

        public IGridContext GridContext { get; } = gridContext;

        public string OperationName { get; } = operationName;

        public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? CompletedAtUtc { get; private set; }

        public bool? IsSuccess { get; private set; }

        public string? ErrorMessage { get; private set; }

        public IReadOnlyDictionary<string, object?> Metadata => _metadata;

        public void Complete()
        {
            if (IsSuccess.HasValue)
            {
                return;
            }

            IsSuccess = true;
            CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Fail(string errorMessage, Exception? exception = null)
        {
            if (IsSuccess.HasValue)
            {
                return;
            }

            IsSuccess = false;
            CompletedAtUtc = DateTimeOffset.UtcNow;
            ErrorMessage = errorMessage;
        }

        public void AddMetadata(string key, object? value)
        {
            _metadata[key] = value;
        }

        public void Dispose()
        {
            if (!IsSuccess.HasValue)
            {
                Complete();
            }
        }
    }

    private sealed class OperationContextAccessor : IOperationContextAccessor
    {
        public IOperationContext? Current { get; set; }
    }
}
