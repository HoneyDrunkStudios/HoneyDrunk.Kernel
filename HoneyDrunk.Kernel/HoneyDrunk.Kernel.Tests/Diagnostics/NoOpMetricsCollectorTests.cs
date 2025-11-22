using FluentAssertions;
using HoneyDrunk.Kernel.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Diagnostics;

public class NoOpMetricsCollectorTests
{
    [Fact]
    public void RecordCounter_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordCounter("test.counter");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCounter_WithValue_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordCounter("test.counter", 5);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCounter_WithTags_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();
        var tags = new[]
        {
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", "value2")
        };

        var act = () => collector.RecordCounter("test.counter", 1, tags);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHistogram_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordHistogram("test.histogram", 42.5);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHistogram_WithTags_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();
        var tags = new[]
        {
            new KeyValuePair<string, object?>("key", "value")
        };

        var act = () => collector.RecordHistogram("test.histogram", 42.5, tags);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordGauge("test.gauge", 100.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_WithTags_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tag1", 1),
            new KeyValuePair<string, object?>("tag2", 2)
        };

        var act = () => collector.RecordGauge("test.gauge", 100.0, tags);

        act.Should().NotThrow();
    }

    [Fact]
    public void MultipleOperations_DoNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () =>
        {
            collector.RecordCounter("counter1");
            collector.RecordCounter("counter2", 10);
            collector.RecordHistogram("histogram1", 5.5);
            collector.RecordGauge("gauge1", 75.0);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordCounter_WithNullTags_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordCounter("test.counter", 1, null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHistogram_WithNullTags_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordHistogram("test.histogram", 42.5, null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_WithNullTags_DoesNotThrow()
    {
        var collector = new NoOpMetricsCollector();

        var act = () => collector.RecordGauge("test.gauge", 100.0, null!);

        act.Should().NotThrow();
    }
}
