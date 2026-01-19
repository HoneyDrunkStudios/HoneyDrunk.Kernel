using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context.Mappers;
using HoneyDrunk.Kernel.Tests.TestHelpers;

namespace HoneyDrunk.Kernel.Tests.Context.Mappers;

/// <summary>
/// Tests for the static JobContextMapper class that initializes GridContext for background job execution.
/// </summary>
public class JobContextMapperTests
{
    private const string TestNodeId = "test-node";
    private const string TestStudioId = "test-studio";
    private const string TestEnvironment = "test-env";

    [Fact]
    public void InitializeForJob_ValidParameters_InitializesContext()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "DataSync");

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().Be("job-123");
        context.NodeId.Should().Be(TestNodeId);
        context.StudioId.Should().Be(TestStudioId);
        context.Environment.Should().Be(TestEnvironment);
    }

    [Fact]
    public void InitializeForJob_SetsJobIdAsCorrelationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        JobContextMapper.InitializeForJob(context, "unique-job-456", "ProcessData");

        // Assert
        context.CorrelationId.Should().Be("unique-job-456");
    }

    [Fact]
    public void InitializeForJob_SetsNullCausationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "IndependentJob");

        // Assert
        context.CausationId.Should().BeNull();
    }

    [Fact]
    public void InitializeForJob_AddsBaggageWithJobTypeAndId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        JobContextMapper.InitializeForJob(context, "job-456", "EmailSender");

        // Assert
        context.Baggage.Should().ContainKey("job-type").WhoseValue.Should().Be("EmailSender");
        context.Baggage.Should().ContainKey("job-id").WhoseValue.Should().Be("job-456");
    }

    [Fact]
    public void InitializeForJob_WithParameters_AddsParametersToBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var parameters = new Dictionary<string, string>
        {
            ["tenant-id"] = "tenant-123",
            ["batch-size"] = "100"
        };

        // Act
        JobContextMapper.InitializeForJob(context, "job-789", "BatchProcess", parameters);

        // Assert
        context.Baggage.Should().ContainKey("job-param-tenant-id").WhoseValue.Should().Be("tenant-123");
        context.Baggage.Should().ContainKey("job-param-batch-size").WhoseValue.Should().Be("100");
    }

    [Fact]
    public void InitializeForJob_WithNullParameters_DoesNotThrow()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "SimpleJob", null);

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("job-type");
        context.Baggage.Should().ContainKey("job-id");
    }

    [Fact]
    public void InitializeForJob_WithEmptyParameters_CreatesContextWithBasicBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var parameters = new Dictionary<string, string>();

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "EmptyParamJob", parameters);

        // Assert
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("job-type");
        context.Baggage.Should().ContainKey("job-id");
    }

    [Fact]
    public void InitializeForJob_WithMultipleParameters_AddsAllToBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var parameters = new Dictionary<string, string>
        {
            ["param1"] = "value1",
            ["param2"] = "value2",
            ["param3"] = "value3"
        };

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "MultiParamJob", parameters);

        // Assert
        context.Baggage.Should().HaveCount(5);
        context.Baggage.Should().ContainKey("job-param-param1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("job-param-param2").WhoseValue.Should().Be("value2");
        context.Baggage.Should().ContainKey("job-param-param3").WhoseValue.Should().Be("value3");
    }

    [Fact]
    public void InitializeForJob_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        using var cts = new CancellationTokenSource();

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "CancellableJob", null, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void InitializeForJob_WithLongJobId_UsesFullIdAsCorrelation()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var longJobId = "very-long-job-identifier-with-many-characters-and-numbers-12345678901234567890";

        // Act
        JobContextMapper.InitializeForJob(context, longJobId, "LongIdJob");

        // Assert
        context.CorrelationId.Should().Be(longJobId);
    }

    [Fact]
    public void InitializeForJob_ParameterPrefixing_PreventsBaggageCollision()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var parameters = new Dictionary<string, string>
        {
            ["job-type"] = "this-should-be-prefixed"
        };

        // Act
        JobContextMapper.InitializeForJob(context, "job-123", "CollisionTestJob", parameters);

        // Assert
        context.Baggage.Should().ContainKey("job-type").WhoseValue.Should().Be("CollisionTestJob");
        context.Baggage.Should().ContainKey("job-param-job-type").WhoseValue.Should().Be("this-should-be-prefixed");
    }

    [Fact]
    public void InitializeForJob_NullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => JobContextMapper.InitializeForJob(null!, "job-123", "SomeJob");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Theory]
    [InlineData("", "JobType")]
    [InlineData("job-id", "")]
    public void InitializeForJob_NullOrWhitespaceParameters_ThrowsArgumentException(string jobId, string jobType)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        var act = () => JobContextMapper.InitializeForJob(context, jobId, jobType);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "JobType")]
    [InlineData("job-id", null)]
    public void InitializeForJob_NullParameters_ThrowsArgumentException(string? jobId, string? jobType)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        var act = () => JobContextMapper.InitializeForJob(context, jobId!, jobType!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InitializeForScheduledJob_ValidParameters_InitializesContext()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = new DateTimeOffset(2026, 1, 19, 10, 30, 0, TimeSpan.Zero);

        // Act
        JobContextMapper.InitializeForScheduledJob(context, "DailyReport", executionTime);

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.NodeId.Should().Be(TestNodeId);
        context.StudioId.Should().Be(TestStudioId);
        context.Environment.Should().Be(TestEnvironment);
    }

    [Fact]
    public void InitializeForScheduledJob_GeneratesUniqueCorrelationId()
    {
        // Arrange
        var context1 = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var context2 = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = DateTimeOffset.UtcNow;

        // Act
        JobContextMapper.InitializeForScheduledJob(context1, "HourlyCleanup", executionTime);
        JobContextMapper.InitializeForScheduledJob(context2, "HourlyCleanup", executionTime);

        // Assert
        context1.CorrelationId.Should().NotBe(context2.CorrelationId);
        context1.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context2.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InitializeForScheduledJob_SetsNullCausationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = DateTimeOffset.UtcNow;

        // Act
        JobContextMapper.InitializeForScheduledJob(context, "IndependentScheduledJob", executionTime);

        // Assert
        context.CausationId.Should().BeNull();
    }

    [Fact]
    public void InitializeForScheduledJob_AddsBaggageWithJobNameAndScheduledTime()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = new DateTimeOffset(2026, 1, 19, 10, 30, 0, TimeSpan.Zero);

        // Act
        JobContextMapper.InitializeForScheduledJob(context, "WeeklyReport", executionTime);

        // Assert
        context.Baggage.Should().ContainKey("job-type").WhoseValue.Should().Be("scheduled");
        context.Baggage.Should().ContainKey("job-name").WhoseValue.Should().Be("WeeklyReport");
        context.Baggage.Should().ContainKey("scheduled-time")
            .WhoseValue.Should().Be(executionTime.ToString("O"));
    }

    [Fact]
    public void InitializeForScheduledJob_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = DateTimeOffset.UtcNow;
        using var cts = new CancellationTokenSource();

        // Act
        JobContextMapper.InitializeForScheduledJob(context, "CancellableScheduledJob", executionTime, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void InitializeForScheduledJob_PreservesExecutionTimeInBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = new DateTimeOffset(2026, 3, 15, 14, 30, 45, TimeSpan.FromHours(2));

        // Act
        JobContextMapper.InitializeForScheduledJob(context, "TimePreservingJob", executionTime);

        // Assert
        context.Baggage.Should().ContainKey("scheduled-time");
        var storedTime = DateTimeOffset.Parse(
            context.Baggage["scheduled-time"],
            provider: System.Globalization.CultureInfo.InvariantCulture);
        storedTime.Should().Be(executionTime);
    }

    [Fact]
    public void InitializeForScheduledJob_NullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => JobContextMapper.InitializeForScheduledJob(null!, "SomeJob", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void InitializeForScheduledJob_NullOrWhitespaceJobName_ThrowsArgumentException(string? jobName)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var executionTime = DateTimeOffset.UtcNow;

        // Act
        var act = () => JobContextMapper.InitializeForScheduledJob(context, jobName!, executionTime);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InitializeFromMetadata_WithCorrelationId_UsesProvidedCorrelationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "existing-corr-123"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.CorrelationId.Should().Be("existing-corr-123");
    }

    [Fact]
    public void InitializeFromMetadata_WithoutCorrelationId_GeneratesNewCorrelationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>();

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InitializeFromMetadata_WithCausationId_SetsCausationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "corr-123",
            [GridHeaderNames.CausationId] = "cause-456"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.CausationId.Should().Be("cause-456");
    }

    [Fact]
    public void InitializeFromMetadata_WithTenantId_SetsTenantId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "corr-123",
            [GridHeaderNames.TenantId] = "tenant-789"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.TenantId.Should().Be("tenant-789");
    }

    [Fact]
    public void InitializeFromMetadata_WithProjectId_SetsProjectId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "corr-123",
            [GridHeaderNames.ProjectId] = "project-abc"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.ProjectId.Should().Be("project-abc");
    }

    [Fact]
    public void InitializeFromMetadata_ExtractsBaggageFromPrefixedKeys()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "corr-123",
            [$"{GridHeaderNames.BaggagePrefix}user-id"] = "user-456",
            [$"{GridHeaderNames.BaggagePrefix}request-source"] = "mobile-app"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.Baggage.Should().ContainKey("user-id").WhoseValue.Should().Be("user-456");
        context.Baggage.Should().ContainKey("request-source").WhoseValue.Should().Be("mobile-app");
    }

    [Fact]
    public void InitializeFromMetadata_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "corr-123"
        };
        using var cts = new CancellationTokenSource();

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void InitializeFromMetadata_WithAllValues_InitializesFullContext()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "full-corr-id",
            [GridHeaderNames.CausationId] = "parent-op-id",
            [GridHeaderNames.TenantId] = "acme-corp",
            [GridHeaderNames.ProjectId] = "project-x",
            [$"{GridHeaderNames.BaggagePrefix}custom-key"] = "custom-value"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().Be("full-corr-id");
        context.CausationId.Should().Be("parent-op-id");
        context.TenantId.Should().Be("acme-corp");
        context.ProjectId.Should().Be("project-x");
        context.Baggage.Should().ContainKey("custom-key").WhoseValue.Should().Be("custom-value");
    }

    [Fact]
    public void InitializeFromMetadata_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var metadata = new Dictionary<string, string>();

        // Act
        var act = () => JobContextMapper.InitializeFromMetadata(null!, metadata);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void InitializeFromMetadata_NullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        // Act
        var act = () => JobContextMapper.InitializeFromMetadata(context, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
    }

    [Fact]
    public void InitializeFromMetadata_EmptyMetadata_GeneratesNewCorrelationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var metadata = new Dictionary<string, string>();

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.CausationId.Should().BeNull();
        context.TenantId.Should().BeNull();
        context.ProjectId.Should().BeNull();
    }

    [Fact]
    public void InitializeFromMetadata_BaggagePrefixCaseInsensitive_ExtractsBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);
        var upperCasePrefix = GridHeaderNames.BaggagePrefix.ToUpperInvariant();
        var metadata = new Dictionary<string, string>
        {
            [GridHeaderNames.CorrelationId] = "corr-123",
            [$"{upperCasePrefix}case-test"] = "case-value"
        };

        // Act
        JobContextMapper.InitializeFromMetadata(context, metadata);

        // Assert
        context.Baggage.Should().ContainKey("case-test").WhoseValue.Should().Be("case-value");
    }
}
