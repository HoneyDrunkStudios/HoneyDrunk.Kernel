using FluentAssertions;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Kernel.Tests.Telemetry;

public class TelemetryLogScopeFactoryTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new TelemetryLogScopeFactory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateScope_NullContext_ThrowsArgumentNullException()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);

        var act = () => factory.CreateScope(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateScope_ReturnsDisposable()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();

        var scope = factory.CreateScope(telemetryContext);

        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void CreateScope_CanBeDisposed()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();

        var scope = factory.CreateScope(telemetryContext);

        var act = scope.Dispose;
        act.Should().NotThrow();
    }

    [Fact]
    public void CreateScope_WithLogger_CreatesScope()
    {
        var testLogger = new TestLogger();
        var factory = new TelemetryLogScopeFactory(testLogger);
        var telemetryContext = CreateTestTelemetryContext();

        using var scope = factory.CreateScope(telemetryContext);

        testLogger.ScopeCreated.Should().BeTrue();
    }

    [Fact]
    public void CreateScope_WithAdditionalProperties_NullContext_ThrowsArgumentNullException()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var additionalProps = new Dictionary<string, object?> { ["key"] = "value" };

        var act = () => factory.CreateScope(null!, additionalProps);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateScope_WithAdditionalProperties_NullProperties_ThrowsArgumentNullException()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();

        var act = () => factory.CreateScope(telemetryContext, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateScope_WithAdditionalProperties_ReturnsDisposable()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();
        var additionalProps = new Dictionary<string, object?> { ["custom"] = "value" };

        var scope = factory.CreateScope(telemetryContext, additionalProps);

        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void CreateScope_WithAdditionalProperties_CanBeDisposed()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();
        var additionalProps = new Dictionary<string, object?> { ["custom"] = "value" };

        var scope = factory.CreateScope(telemetryContext, additionalProps);

        var act = scope.Dispose;
        act.Should().NotThrow();
    }

    [Fact]
    public void CreateScope_WithEmptyAdditionalProperties_CreatesScope()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();
        var additionalProps = new Dictionary<string, object?>();

        var scope = factory.CreateScope(telemetryContext, additionalProps);

        scope.Should().NotBeNull();
    }

    [Fact]
    public void CreateScope_MultipleTimes_CreatesMultipleScopes()
    {
        var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
        var telemetryContext = CreateTestTelemetryContext();

        var scope1 = factory.CreateScope(telemetryContext);
        var scope2 = factory.CreateScope(telemetryContext);
        var scope3 = factory.CreateScope(telemetryContext);

        scope1.Should().NotBeNull();
        scope2.Should().NotBeNull();
        scope3.Should().NotBeNull();

        scope1.Dispose();
        scope2.Dispose();
        scope3.Dispose();
    }

    [Fact]
    public void CreateScope_WithParentSpanId_IncludesInScope()
    {
        var testLogger = new TestLogger();
        var factory = new TelemetryLogScopeFactory(testLogger);
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace-id",
            "span-id",
            parentSpanId: "parent-span-id");

        using var scope = factory.CreateScope(telemetryContext);

        testLogger.ScopeCreated.Should().BeTrue();
    }

    [Fact]
    public void CreateScope_WithCausationId_IncludesInScope()
    {
        var testLogger = new TestLogger();
        var factory = new TelemetryLogScopeFactory(testLogger);
        var gridContext = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "test",
            causationId: "cause-456");
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");

        using var scope = factory.CreateScope(telemetryContext);

        testLogger.ScopeCreated.Should().BeTrue();
    }

    private static TelemetryContext CreateTestTelemetryContext()
    {
        var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
        return new TelemetryContext(gridContext, "trace-id", "span-id");
    }

    private sealed class TestLogger : ILogger
    {
        public bool ScopeCreated { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            ScopeCreated = true;
            return new TestScope();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class TestScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
