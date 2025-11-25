using FluentAssertions;
using HoneyDrunk.Kernel.Context.Mappers;

namespace HoneyDrunk.Kernel.Tests.Context.Mappers;

public class JobContextMapperTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesMapper()
    {
        // Act
        var mapper = new JobContextMapper("node-id", "studio-id", "production");

        // Assert
        mapper.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "studio", "env")]
    [InlineData("node", "", "env")]
    [InlineData("node", "studio", "")]
    public void Constructor_NullOrWhitespaceParameters_ThrowsArgumentException(
        string nodeId,
        string studioId,
        string environment)
    {
        // Act
        var act = () => new JobContextMapper(nodeId, studioId, environment);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "studio", "env")]
    [InlineData("node", null, "env")]
    [InlineData("node", "studio", null)]
    public void Constructor_NullParameters_ThrowsArgumentException(
        string? nodeId,
        string? studioId,
        string? environment)
    {
        // Act
        var act = () => new JobContextMapper(nodeId!, studioId!, environment!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapFromJob_ValidParameters_CreatesContext()
    {
        // Arrange
        var mapper = new JobContextMapper("test-node", "test-studio", "production");

        // Act
        var context = mapper.MapFromJob("job-123", "DataSync");

        // Assert
        context.Should().NotBeNull();
        context.CorrelationId.Should().Be("job-123");
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("production");
        context.CausationId.Should().BeNull();
    }

    [Theory]
    [InlineData("", "JobType")]
    [InlineData("job-id", "")]
    public void MapFromJob_NullOrWhitespaceParameters_ThrowsArgumentException(
        string jobId,
        string jobType)
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");

        // Act
        var act = () => mapper.MapFromJob(jobId, jobType);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "JobType")]
    [InlineData("job-id", null)]
    public void MapFromJob_NullParameters_ThrowsArgumentException(
        string? jobId,
        string? jobType)
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");

        // Act
        var act = () => mapper.MapFromJob(jobId!, jobType!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapFromJob_UsesJobIdAsCorrelationId()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");

        // Act
        var context = mapper.MapFromJob("unique-job-123", "DataProcess");

        // Assert
        context.CorrelationId.Should().Be("unique-job-123");
    }

    [Fact]
    public void MapFromJob_AddsBaggageWithJobTypeAndId()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");

        // Act
        var context = mapper.MapFromJob("job-456", "EmailSender");

        // Assert
        context.Baggage.Should().ContainKey("job-type").WhoseValue.Should().Be("EmailSender");
        context.Baggage.Should().ContainKey("job-id").WhoseValue.Should().Be("job-456");
    }

    [Fact]
    public void MapFromJob_WithParameters_AddsParametersToBaggage()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var parameters = new Dictionary<string, string>
        {
            ["tenant-id"] = "tenant-123",
            ["batch-size"] = "100"
        };

        // Act
        var context = mapper.MapFromJob("job-789", "BatchProcess", parameters);

        // Assert
        context.Baggage.Should().ContainKey("job-param-tenant-id").WhoseValue.Should().Be("tenant-123");
        context.Baggage.Should().ContainKey("job-param-batch-size").WhoseValue.Should().Be("100");
    }

    [Fact]
    public void MapFromJob_WithNullParameters_DoesNotThrow()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");

        // Act
        var context = mapper.MapFromJob("job-123", "SimpleJob", null);

        // Assert
        context.Should().NotBeNull();
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("job-type");
        context.Baggage.Should().ContainKey("job-id");
    }

    [Fact]
    public void MapFromJob_WithEmptyParameters_CreatesContextWithBasicBaggage()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var parameters = new Dictionary<string, string>();

        // Act
        var context = mapper.MapFromJob("job-123", "EmptyParamJob", parameters);

        // Assert
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("job-type");
        context.Baggage.Should().ContainKey("job-id");
    }

    [Fact]
    public void MapFromJob_WithMultipleParameters_AddsAllToBaggage()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var parameters = new Dictionary<string, string>
        {
            ["param1"] = "value1",
            ["param2"] = "value2",
            ["param3"] = "value3"
        };

        // Act
        var context = mapper.MapFromJob("job-123", "MultiParamJob", parameters);

        // Assert
        context.Baggage.Should().HaveCount(5);
        context.Baggage.Should().ContainKey("job-param-param1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("job-param-param2").WhoseValue.Should().Be("value2");
        context.Baggage.Should().ContainKey("job-param-param3").WhoseValue.Should().Be("value3");
    }

    [Fact]
    public void MapFromJob_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        using var cts = new CancellationTokenSource();

        // Act
        var context = mapper.MapFromJob("job-123", "CancellableJob", null, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void MapFromScheduledJob_ValidParameters_CreatesContext()
    {
        // Arrange
        var mapper = new JobContextMapper("test-node", "test-studio", "production");
        var executionTime = new DateTimeOffset(2025, 1, 11, 10, 30, 0, TimeSpan.Zero);

        // Act
        var context = mapper.MapFromScheduledJob("DailyReport", executionTime);

        // Assert
        context.Should().NotBeNull();
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("production");
        context.CausationId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MapFromScheduledJob_NullOrWhitespaceJobName_ThrowsArgumentException(string? jobName)
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var executionTime = DateTimeOffset.UtcNow;

        // Act
        var act = () => mapper.MapFromScheduledJob(jobName!, executionTime);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapFromScheduledJob_GeneratesUniqueCorrelationId()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var executionTime = DateTimeOffset.UtcNow;

        // Act
        var context1 = mapper.MapFromScheduledJob("HourlyCleanup", executionTime);
        var context2 = mapper.MapFromScheduledJob("HourlyCleanup", executionTime);

        // Assert
        context1.CorrelationId.Should().NotBe(context2.CorrelationId);
        context1.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context2.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void MapFromScheduledJob_AddsBaggageWithJobNameAndScheduledTime()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var executionTime = new DateTimeOffset(2025, 1, 11, 10, 30, 0, TimeSpan.Zero);

        // Act
        var context = mapper.MapFromScheduledJob("WeeklyReport", executionTime);

        // Assert
        context.Baggage.Should().ContainKey("job-type").WhoseValue.Should().Be("scheduled");
        context.Baggage.Should().ContainKey("job-name").WhoseValue.Should().Be("WeeklyReport");
        context.Baggage.Should().ContainKey("scheduled-time")
            .WhoseValue.Should().Be(executionTime.ToString("O"));
    }

    [Fact]
    public void MapFromScheduledJob_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var executionTime = DateTimeOffset.UtcNow;
        using var cts = new CancellationTokenSource();

        // Act
        var context = mapper.MapFromScheduledJob("CancellableScheduledJob", executionTime, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void MapFromScheduledJob_PreservesExecutionTimeInBaggage()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var executionTime = new DateTimeOffset(2025, 3, 15, 14, 30, 45, TimeSpan.FromHours(2));

        // Act
        var context = mapper.MapFromScheduledJob("TimePreservingJob", executionTime);

        // Assert
        context.Baggage.Should().ContainKey("scheduled-time");
        var storedTime = DateTimeOffset.Parse(context.Baggage["scheduled-time"], provider: System.Globalization.CultureInfo.InvariantCulture);
        storedTime.Should().Be(executionTime);
    }

    [Fact]
    public void MapFromJob_SetsNullCausationId()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");

        // Act
        var context = mapper.MapFromJob("job-123", "IndependentJob");

        // Assert
        context.CausationId.Should().BeNull();
    }

    [Fact]
    public void MapFromScheduledJob_SetsNullCausationId()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var executionTime = DateTimeOffset.UtcNow;

        // Act
        var context = mapper.MapFromScheduledJob("IndependentScheduledJob", executionTime);

        // Assert
        context.CausationId.Should().BeNull();
    }

    [Fact]
    public void MapFromJob_WithLongJobId_UsesFullIdAsCorrelation()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var longJobId = "very-long-job-identifier-with-many-characters-and-numbers-12345678901234567890";

        // Act
        var context = mapper.MapFromJob(longJobId, "LongIdJob");

        // Assert
        context.CorrelationId.Should().Be(longJobId);
    }

    [Fact]
    public void MapFromJob_ParameterPrefixing_PreventsBaggageCollision()
    {
        // Arrange
        var mapper = new JobContextMapper("node", "studio", "env");
        var parameters = new Dictionary<string, string>
        {
            ["job-type"] = "this-should-be-prefixed"
        };

        // Act
        var context = mapper.MapFromJob("job-123", "CollisionTestJob", parameters);

        // Assert
        context.Baggage.Should().ContainKey("job-type").WhoseValue.Should().Be("CollisionTestJob");
        context.Baggage.Should().ContainKey("job-param-job-type").WhoseValue.Should().Be("this-should-be-prefixed");
    }
}
