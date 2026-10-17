using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context.Mappers;
using HoneyDrunk.Kernel.Telemetry;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using HoneyDrunk.Kernel.Transport;
using System.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Telemetry;

public class GridActivitySourceTests
{
    private const string TraceId = "0af7651916cd43dd8448eb211c80319c";
    private const string ParentSpanId = "b7ad6b7169203331";
    private const string TraceState = "vendor=value";

    [Fact]
    public void SourceName_HasCorrectValue()
    {
        GridActivitySource.SourceName.Should().Be("HoneyDrunk.Grid");
    }

    [Fact]
    public void Version_HasCorrectValue()
    {
        GridActivitySource.Version.Should().Be("0.3.0");
    }

    [Fact]
    public void Instance_IsNotNull()
    {
        GridActivitySource.Instance.Should().NotBeNull();
        GridActivitySource.Instance.Name.Should().Be("HoneyDrunk.Grid");
        GridActivitySource.Instance.Version.Should().Be("0.3.0");
    }

    [Fact]
    public void Instance_IsSingleton()
    {
        var instance1 = GridActivitySource.Instance;
        var instance2 = GridActivitySource.Instance;

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void StartActivity_WithValidParameters_EnrichesWithGridContext()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("TestOperation");
        activity.Tags.Should().Contain(t => t.Key == "hd.correlation_id" && t.Value == "corr-123");
        activity.Tags.Should().Contain(t => t.Key == "hd.node_id" && t.Value == "test-node");
        activity.Tags.Should().Contain(t => t.Key == "hd.studio_id" && t.Value == "test-studio");
        activity.Tags.Should().Contain(t => t.Key == "hd.environment" && t.Value == "test-env");
    }

    [Fact]
    public void StartActivity_WithCausationId_IncludesCausationTag()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "cause-456");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "hd.causation_id" && t.Value == "cause-456");
    }

    [Fact]
    public void StartActivity_WithBaggage_IncludesBaggageTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var baggage = new Dictionary<string, string>
        {
            ["tenant_id"] = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            ["user_id"] = "user-456"
        };
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: baggage);

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "hd.baggage.tenant_id" && t.Value == "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        activity!.Tags.Should().Contain(t => t.Key == "hd.baggage.user_id" && t.Value == "user-456");
    }

    [Fact]
    public void StartActivity_WithAdditionalTags_IncludesCustomTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");
        var customTags = new Dictionary<string, object?>
        {
            ["custom.key1"] = "value1",
            ["custom.key2"] = 42
        };

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, tags: customTags);

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "custom.key1" && t.Value == "value1");
        activity!.Tags.Should().Contain(t => t.Key == "custom.key2");
    }

    [Fact]
    public void StartActivity_WithActivityKind_SetsCorrectKind()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, ActivityKind.Server);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Server);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void StartActivity_WithNullOrWhitespaceOperationName_ThrowsArgumentException(string? operationName)
    {
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var act = () => GridActivitySource.StartActivity(operationName!, gridContext);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartActivity_WithNullGridContext_ThrowsArgumentNullException()
    {
        var act = () => GridActivitySource.StartActivity("TestOperation", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartHttpActivity_EnrichesWithHttpTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartHttpActivity("GET", "/api/users", gridContext);

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("HTTP GET /api/users");
        activity.Kind.Should().Be(ActivityKind.Server);
        activity.Tags.Should().Contain(t => t.Key == "http.method" && t.Value == "GET");
        activity.Tags.Should().Contain(t => t.Key == "http.target" && t.Value == "/api/users");
    }

    [Fact]
    public void StartDatabaseActivity_EnrichesWithDatabaseTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartDatabaseActivity("query", "users", gridContext);

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("DB query users");
        activity.Kind.Should().Be(ActivityKind.Client);
        activity.Tags.Should().Contain(t => t.Key == "db.operation" && t.Value == "query");
        activity.Tags.Should().Contain(t => t.Key == "db.table" && t.Value == "users");
    }

    [Fact]
    public void StartMessageActivity_EnrichesWithMessagingTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartMessageActivity("OrderCreated", "orders-queue", gridContext, ActivityKind.Producer);

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("Message OrderCreated");
        activity.Kind.Should().Be(ActivityKind.Producer);
        activity.Tags.Should().Contain(t => t.Key == "messaging.message_type" && t.Value == "OrderCreated");
        activity.Tags.Should().Contain(t => t.Key == "messaging.destination" && t.Value == "orders-queue");
    }

    [Fact]
    public void RecordException_WithActivity_SetsErrorStatus()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");
        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);
        var exception = new InvalidOperationException("Test error");

        GridActivitySource.RecordException(activity, exception);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Test error");
        activity.Tags.Should().Contain(t => t.Key == "exception.type" && t.Value == "System.InvalidOperationException");
        activity.Tags.Should().Contain(t => t.Key == "exception.message" && t.Value == "Test error");
    }

    [Fact]
    public void RecordException_WithNullActivity_DoesNotThrow()
    {
        var exception = new InvalidOperationException("Test error");

        var act = () => GridActivitySource.RecordException(null, exception);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_WithNullException_DoesNotThrow()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");
        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        var act = () => GridActivitySource.RecordException(activity, null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetSuccess_WithActivity_SetsOkStatus()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");
        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        GridActivitySource.SetSuccess(activity);

        activity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SetSuccess_WithNullActivity_DoesNotThrow()
    {
        var act = () => GridActivitySource.SetSuccess(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void StartActivity_WithEmptyBaggage_DoesNotAddBaggageTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: new Dictionary<string, string>());

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().NotBeNull();
        activity!.Tags.Should().NotContain(t => t.Key.StartsWith("hd.baggage."));
    }

    [Fact]
    public void StartActivity_WithNullCustomTags_DoesNotThrow()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, tags: null);

        activity.Should().NotBeNull();
    }

    [Fact]
    public void StartActivity_WithClientKind_SetsClientKind()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, ActivityKind.Client);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Client);
    }

    [Fact]
    public void StartActivity_WithProducerKind_SetsProducerKind()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, ActivityKind.Producer);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Producer);
    }

    [Fact]
    public void StartActivity_WithConsumerKind_SetsConsumerKind()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, ActivityKind.Consumer);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Consumer);
    }

    [Fact]
    public void StartActivity_WithInternalKind_SetsInternalKind()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, ActivityKind.Internal);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Internal);
    }

    [Fact]
    public void StartActivity_WithoutListener_ReturnsNull()
    {
        // No listener registered, activity should not be created
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartHttpActivity_WithoutListener_ReturnsNull()
    {
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var activity = GridActivitySource.StartHttpActivity("GET", "/api/test", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartDatabaseActivity_WithoutListener_ReturnsNull()
    {
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var activity = GridActivitySource.StartDatabaseActivity("select", "users", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartMessageActivity_WithoutListener_ReturnsNull()
    {
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var activity = GridActivitySource.StartMessageActivity("Event", "queue", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void RecordException_WithExceptionWithStackTrace_IncludesStackTrace()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");
        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        var ex = CaptureThrownException();
        GridActivitySource.RecordException(activity, ex);

        activity!.Tags.Should().Contain(t => t.Key == "exception.stacktrace");
    }

    [Fact]
    public void StartActivity_WithNullTagValue_HandlesGracefully()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");
        var customTags = new Dictionary<string, object?>
        {
            ["key-with-null"] = null,
            ["key-with-value"] = "value"
        };

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, tags: customTags);

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "key-with-value");
    }

    [Fact]
    public void StartMessageActivity_WithConsumerKind_SetsConsumerKind()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        using var activity = GridActivitySource.StartMessageActivity("OrderProcessed", "orders-queue", gridContext, ActivityKind.Consumer);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Consumer);
    }

    [Theory]
    [InlineData(false, "00")]
    [InlineData(false, "01")]
    [InlineData(true, "00")]
    [InlineData(true, "01")]
    public async Task StartActivity_WithAmbientParent_PreservesTraceContext(bool useKernelSource, string flags)
    {
        using var listener = CreateListener();
        using var parent = new Activity("remote-request")
            .SetParentId($"00-{TraceId}-{ParentSpanId}-{flags}")
            .Start();
        parent.TraceStateString = TraceState;
        var grid = GridContextTestHelper.CreateDefault(correlationId: "business-correlation");

        using var child = useKernelSource
            ? HoneyDrunkTelemetry.StartActivity("local-operation", grid)
            : GridActivitySource.StartActivity("local-operation", grid);
        await Task.Yield();

        child.Should().NotBeNull();
        child!.TraceId.Should().Be(parent.TraceId);
        child.ParentSpanId.Should().Be(parent.SpanId);
        child.SpanId.Should().NotBe(parent.SpanId);
        child.ActivityTraceFlags.Should().Be(parent.ActivityTraceFlags);
        child.TraceStateString.Should().Be(TraceState);
        child.GetTagItem("hd.correlation_id").Should().Be("business-correlation");
        Activity.Current.Should().BeSameAs(child);
        grid.CorrelationId.Should().Be("business-correlation");
    }

    [Theory]
    [InlineData(false, "00")]
    [InlineData(false, "01")]
    [InlineData(true, "00")]
    [InlineData(true, "01")]
    public void Bind_WithActiveTrace_RoundTripsIndependentTraceAndBusinessContext(bool isJob, string flags)
    {
        using var listener = CreateListener();
        var grid = GridContextTestHelper.CreateInitialized(
            "business-correlation", "test-node", "test-studio", "test-env", causationId: "business-parent");
        Dictionary<string, string> metadata;
        ActivityContext producerContext;
        using (var producer = new Activity("send")
            .SetParentId($"00-{TraceId}-{ParentSpanId}-{flags}")
            .Start())
        {
            producer.TraceStateString = TraceState;
            producerContext = producer.Context;
            metadata = BindMetadata(grid, isJob);
            metadata["traceparent"].Should().Be(producer.Id);
            metadata["tracestate"].Should().Be(TraceState);
        }

        var receivedGrid = GridContextTestHelper.CreateUninitialized();
        if (isJob)
        {
            JobContextMapper.InitializeFromMetadata(receivedGrid, metadata);
        }
        else
        {
            MessagingContextMapper.InitializeFromMessage(receivedGrid, metadata);
        }

        ActivityContext.TryParse(metadata["traceparent"], metadata["tracestate"], isRemote: true, out var remoteParent)
            .Should().BeTrue();
        using var consumer = GridActivitySource.Instance.StartActivity("receive", ActivityKind.Consumer, remoteParent);
        using var work = GridActivitySource.StartActivity("handle", receivedGrid);

        consumer.Should().NotBeNull();
        consumer!.TraceId.Should().Be(producerContext.TraceId);
        consumer.ParentSpanId.Should().Be(producerContext.SpanId);
        consumer.ActivityTraceFlags.Should().Be(producerContext.TraceFlags);
        consumer.TraceStateString.Should().Be(TraceState);
        work.Should().NotBeNull();
        work!.TraceId.Should().Be(producerContext.TraceId);
        work.ParentSpanId.Should().Be(consumer.SpanId);
        work.TraceStateString.Should().Be(TraceState);
        receivedGrid.CorrelationId.Should().Be("business-correlation");
        receivedGrid.CausationId.Should().Be("business-parent");
        receivedGrid.CorrelationId.Should().NotBe(work.TraceId.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bind_WithoutActiveTrace_DoesNotInventTraceContext(bool isJob)
    {
        var previous = Activity.Current;
        try
        {
            Activity.Current = null;
            var metadata = BindMetadata(GridContextTestHelper.CreateDefault(), isJob);

            metadata.Should().NotContainKey("traceparent");
            metadata.Should().NotContainKey("tracestate");
            metadata[GridHeaderNames.CorrelationId].Should().Be("test-correlation-id");
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static InvalidOperationException CaptureThrownException()
    {
        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is GridActivitySource.SourceName or "HoneyDrunk.Kernel",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static Dictionary<string, string> BindMetadata(IGridContext grid, bool isJob)
    {
        if (isJob)
        {
            var metadata = new Dictionary<string, string>();
            new JobMetadataBinder().Bind(metadata, grid);
            return metadata;
        }

        var properties = new Dictionary<string, object>();
        new MessagePropertiesBinder().Bind(properties, grid);
        return properties.ToDictionary(pair => pair.Key, pair => (string)pair.Value);
    }
}
