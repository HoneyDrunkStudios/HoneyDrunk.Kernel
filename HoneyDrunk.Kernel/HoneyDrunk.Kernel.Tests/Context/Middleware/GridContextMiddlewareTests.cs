using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Context.Middleware;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Kernel.Tests.Context.Middleware;

/// <summary>
/// Tests for <see cref="GridContextMiddleware"/>.
/// </summary>
/// <remarks>
/// The middleware architecture (v0.3.0):
/// <list type="bullet">
/// <item>Constructor takes RequestDelegate (next) and ILogger.</item>
/// <item>InvokeAsync takes only HttpContext - resolves services from RequestServices.</item>
/// <item>Calls gridContext.Initialize() with values extracted from HTTP headers.</item>
/// <item>Calls gridContext.MarkDisposed() in finally block.</item>
/// <item>Does NOT create GridContext - expects DI to have already created the scoped instance.</item>
/// </list>
/// </remarks>
public class GridContextMiddlewareTests
{
    private const string TestNodeId = "test-node";
    private const string TestStudioId = "test-studio";
    private const string TestEnvironment = "test-env";

    [Fact]
    public async Task InvokeAsync_WithValidRequest_InitializesGridContext()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "test-corr-123";
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";

        bool? capturedIsInitialized = null;
        string? capturedCorrelationId = null;
        string? capturedNodeId = null;
        string? capturedStudioId = null;
        string? capturedEnvironment = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedIsInitialized = gridContext.IsInitialized;
                capturedCorrelationId = gridContext.CorrelationId;
                capturedNodeId = gridContext.NodeId;
                capturedStudioId = gridContext.StudioId;
                capturedEnvironment = gridContext.Environment;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedIsInitialized.Should().BeTrue();
        capturedCorrelationId.Should().Be("test-corr-123");
        capturedNodeId.Should().Be(TestNodeId);
        capturedStudioId.Should().Be(TestStudioId);
        capturedEnvironment.Should().Be(TestEnvironment);
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationHeader_GeneratesNewCorrelation()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/orders";

        bool? capturedIsInitialized = null;
        string? capturedCorrelationId = null;
        string? capturedNodeId = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedIsInitialized = gridContext.IsInitialized;
                capturedCorrelationId = gridContext.CorrelationId;
                capturedNodeId = gridContext.NodeId;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedIsInitialized.Should().BeTrue();
        capturedCorrelationId.Should().NotBeNullOrWhiteSpace();
        capturedNodeId.Should().Be(TestNodeId);
    }

    [Fact]
    public async Task InvokeAsync_CreatesOperationContextWithMetadata()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Method = "PUT";
        httpContext.Request.Path = "/api/users/123";
        httpContext.TraceIdentifier = "trace-456";

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        opFactory.LastCreatedOperationName.Should().Be("HttpRequest");
        opFactory.LastCreatedMetadata.Should().ContainKey("http.method");
        opFactory.LastCreatedMetadata!["http.method"].Should().Be("PUT");
        opFactory.LastCreatedMetadata.Should().ContainKey("http.path");
        opFactory.LastCreatedMetadata["http.path"].Should().Be("/api/users/123");
        opFactory.LastCreatedMetadata.Should().ContainKey("http.request_id");
        opFactory.LastCreatedMetadata["http.request_id"].Should().Be("trace-456");
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_FailsOperationAndRethrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);

        var middleware = new GridContextMiddleware(
            next: _ => throw expectedException,
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        var act = async () => await middleware.InvokeAsync(httpContext);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        opFactory.LastCreatedOperation.Should().NotBeNull();
        opFactory.LastCreatedOperation!.IsSuccess.Should().BeFalse();
        opFactory.LastCreatedOperation.ErrorMessage.Should().Be("Unhandled exception");
    }

    [Fact]
    public async Task InvokeAsync_MarksGridContextDisposedAfterRequest()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        gridContext.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ClearsOperationContextAccessorAfterRequest()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        opAccessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_MarksGridContextDisposedEvenOnException()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);

        var middleware = new GridContextMiddleware(
            next: _ => throw new InvalidOperationException("Test error"),
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        try
        {
            await middleware.InvokeAsync(httpContext);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert
        gridContext.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithVeryLongHeaders_TruncatesToMaxLength()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        var longValue = new string('x', 300);
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = longValue;

        int? capturedCorrelationIdLength = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedCorrelationIdLength = gridContext.CorrelationId.Length;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedCorrelationIdLength.Should().BeLessThanOrEqualTo(256);
    }

    [Fact]
    public async Task InvokeAsync_WithCausationHeader_PropagatesCausationId()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "corr-123";
        httpContext.Request.Headers[GridHeaderNames.CausationId] = "cause-456";

        string? capturedCorrelationId = null;
        string? capturedCausationId = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedCorrelationId = gridContext.CorrelationId;
                capturedCausationId = gridContext.CausationId;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedCorrelationId.Should().Be("corr-123");
        capturedCausationId.Should().Be("cause-456");
    }

    [Fact]
    public async Task InvokeAsync_WithTenantAndProjectHeaders_PropagatesIdentifiers()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "corr-123";
        httpContext.Request.Headers[GridHeaderNames.TenantId] = "tenant-abc";
        httpContext.Request.Headers[GridHeaderNames.ProjectId] = "project-xyz";

        string? capturedTenantId = null;
        string? capturedProjectId = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedTenantId = gridContext.TenantId;
                capturedProjectId = gridContext.ProjectId;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedTenantId.Should().Be("tenant-abc");
        capturedProjectId.Should().Be("project-xyz");
    }

    [Fact]
    public async Task InvokeAsync_SuccessfulRequest_CompletesOperation()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);

        var middleware = new GridContextMiddleware(
            next: async _ => await Task.CompletedTask,
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        opFactory.LastCreatedOperation.Should().NotBeNull();
        opFactory.LastCreatedOperation!.IsSuccess.Should().BeTrue();
        opFactory.LastCreatedOperation.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithBaggageHeaders_PropagatesBaggage()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Headers[$"{GridHeaderNames.BaggagePrefix}tenant-id"] = "tenant-123";
        httpContext.Request.Headers[$"{GridHeaderNames.BaggagePrefix}user-id"] = "user-456";

        IReadOnlyDictionary<string, string>? capturedBaggage = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedBaggage = new Dictionary<string, string>(gridContext.Baggage);
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedBaggage.Should().ContainKey("tenant-id").WhoseValue.Should().Be("tenant-123");
        capturedBaggage.Should().ContainKey("user-id").WhoseValue.Should().Be("user-456");
    }

    [Fact]
    public async Task InvokeAsync_WithW3CBaggageHeader_ParsesBaggage()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Headers[GridHeaderNames.Baggage] = "key1=value1,key2=value2";

        IReadOnlyDictionary<string, string>? capturedBaggage = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedBaggage = new Dictionary<string, string>(gridContext.Baggage);
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        capturedBaggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        capturedBaggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public async Task InvokeAsync_WithTraceparentHeader_ExtractsCorrelationId()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);

        // W3C traceparent format: version-trace_id-span_id-trace_flags
        httpContext.Request.Headers[GridHeaderNames.TraceParent] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        string? capturedCorrelationId = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedCorrelationId = gridContext.CorrelationId;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert - trace_id is extracted as correlation id
        capturedCorrelationId.Should().Be("4bf92f3577b34da6a3ce929d0e0e4736");
    }

    [Fact]
    public async Task InvokeAsync_CorrelationIdHeaderTakesPrecedenceOverTraceparent()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var nodeContext = CreateTestNodeContext();
        var opAccessor = new TestOperationContextAccessor();
        var opFactory = new TestOperationContextFactory();

        var httpContext = CreateHttpContextWithServices(gridContext, nodeContext, opAccessor, opFactory);
        httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "explicit-corr-id";
        httpContext.Request.Headers[GridHeaderNames.TraceParent] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        string? capturedCorrelationId = null;

        var middleware = new GridContextMiddleware(
            next: async _ =>
            {
                capturedCorrelationId = gridContext.CorrelationId;
                await Task.CompletedTask;
            },
            logger: NullLogger<GridContextMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert - X-Correlation-Id takes precedence
        capturedCorrelationId.Should().Be("explicit-corr-id");
    }

    private static DefaultHttpContext CreateHttpContextWithServices(
        GridContext gridContext,
        INodeContext nodeContext,
        IOperationContextAccessor opAccessor,
        IOperationContextFactory opFactory)
    {
        var services = new ServiceCollection();
        services.AddSingleton(gridContext);
        services.AddSingleton(nodeContext);
        services.AddSingleton(opAccessor);
        services.AddSingleton(opFactory);

        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };

        return httpContext;
    }

    private static TestNodeContext CreateTestNodeContext()
    {
        return new TestNodeContext
        {
            NodeId = TestNodeId,
            Version = "1.0.0",
            StudioId = TestStudioId,
            Environment = TestEnvironment,
        };
    }

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

    private sealed class TestOperationContextAccessor : IOperationContextAccessor
    {
        public IOperationContext? Current { get; set; }
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

            // Create a minimal GridContext for the operation context
            var gridContext = GridContextTestHelper.CreateDefault();

            // Generate operationId for OperationContext (which now owns it)
            var operationId = Ulid.NewUlid().ToString();
            LastCreatedOperation = new TestOperationContext(gridContext, operationName, operationId, metadata);
            return LastCreatedOperation;
        }
    }

    private sealed class TestOperationContext(IGridContext gridContext, string operationName, string operationId, IReadOnlyDictionary<string, object?>? metadata) : IOperationContext
    {
        private readonly Dictionary<string, object?> _metadata = metadata != null ? new Dictionary<string, object?>(metadata) : [];

        public IGridContext GridContext { get; } = gridContext;

        public string OperationName { get; } = operationName;

        public string OperationId { get; } = operationId;

        public string CorrelationId => GridContext.CorrelationId;

        public string? CausationId => GridContext.CausationId;

        public string? TenantId => GridContext.TenantId;

        public string? ProjectId => GridContext.ProjectId;

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
}
