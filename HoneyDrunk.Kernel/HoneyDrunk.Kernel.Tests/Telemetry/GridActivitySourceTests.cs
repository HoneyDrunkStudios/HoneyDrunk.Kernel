using FluentAssertions;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Telemetry;
using System.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Telemetry;

public class GridActivitySourceTests
{
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env", "cause-456");

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
            ["tenant_id"] = "tenant-123",
            ["user_id"] = "user-456"
        };
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env", baggage: baggage);

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "hd.baggage.tenant_id" && t.Value == "tenant-123");
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env", baggage: new Dictionary<string, string>());

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext, ActivityKind.Internal);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Internal);
    }

    [Fact]
    public void StartActivity_WithoutListener_ReturnsNull()
    {
        // No listener registered, activity should not be created
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

        var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartHttpActivity_WithoutListener_ReturnsNull()
    {
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

        var activity = GridActivitySource.StartHttpActivity("GET", "/api/test", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartDatabaseActivity_WithoutListener_ReturnsNull()
    {
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

        var activity = GridActivitySource.StartDatabaseActivity("select", "users", gridContext);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartMessageActivity_WithoutListener_ReturnsNull()
    {
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");
        using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);

        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (Exception ex)
        {
            GridActivitySource.RecordException(activity, ex);

            activity!.Tags.Should().Contain(t => t.Key == "exception.stacktrace");
        }
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");
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

        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test-env");

        using var activity = GridActivitySource.StartMessageActivity("OrderProcessed", "orders-queue", gridContext, ActivityKind.Consumer);

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Consumer);
    }
}
