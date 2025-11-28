# 📈 Diagnostics - Metrics and Observability

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IMetricsCollector.cs](#imetricscollectorcs)
- [NoOpMetricsCollector](#noopmetricscollector-implementation)
- [Relationship to Telemetry](#relationship-to-telemetry)
- [Summary](#summary)

---

## Overview

Abstractions for recording application metrics without coupling to specific telemetry systems.

**What Diagnostics Is:** **Telemetry covers traces and logs**. **Diagnostics covers raw metrics** (counters, histograms, gauges) behind a simple abstraction that can be wired to OpenTelemetry, Prometheus, Application Insights, or left as a no-op.

**Location:** `HoneyDrunk.Kernel.Abstractions/Diagnostics/`

**Key Concepts:**
- **Counter** - Monotonically increasing event count (requests processed, errors encountered)
- **Histogram** - Distribution of values (latency, payment amounts, message sizes)
- **Gauge** - Sampled measurement at a point in time (memory usage, queue depth, active connections)

**Observability Trinity:**
- **Telemetry** - Traces (what happened when) + Logs (structured events)
- **Diagnostics** - Metrics (how much, how often, how long)
- **Health** - Service status (working, impaired, failed)

[↑ Back to top](#table-of-contents)

---

## IMetricsCollector.cs

**What it is:** Minimal interface for recording metrics without coupling to a specific observability backend.

**Location:** `HoneyDrunk.Kernel.Abstractions/Diagnostics/IMetricsCollector.cs`

```csharp
public interface IMetricsCollector
{
    void RecordCounter(string name, long value, params KeyValuePair<string, object?>[] tags);
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);
}
```

### Method Semantics

| Method | Semantic Intent | Use Case | Example |
|--------|-----------------|----------|---------|
| **RecordCounter** | Monotonically increasing event count | Requests processed, errors, events | `RecordCounter("payments.processed", 1)` |
| **RecordHistogram** | Distribution of values | Latency, amounts, sizes | `RecordHistogram("payments.amount", 99.99)` |
| **RecordGauge** | Sampled measurement at a point in time | Memory usage, queue depth, active connections | `RecordGauge("queue.depth", 42)` |

### Design Expectations

**Thread Safety:**
All `IMetricsCollector` implementations **must be safe to call from multiple threads concurrently**. The interface itself cannot enforce this, but it is a required contract.

**Tag Conventions:**
Implementations are encouraged to **reuse `TelemetryTags`** where possible so metrics and traces can be correlated by the same dimensions (e.g., `hd.node_id`, `hd.environment`, `hd.studio_id`). Custom tags (e.g., `currency`, `region`) are fine, but align with Grid conventions when applicable.

### Usage Example

```csharp
public class PaymentProcessor(IMetricsCollector metrics)
{
    public async Task ProcessAsync(decimal amount, string currency)
    {
        // Counter: how many payments processed
        metrics.RecordCounter("payments.processed", 1,
            new KeyValuePair<string, object?>("currency", currency),
            new KeyValuePair<string, object?>(TelemetryTags.NodeId, "payment-node"));
            
        // Histogram: distribution of payment amounts
        metrics.RecordHistogram("payments.amount", (double)amount,
            new KeyValuePair<string, object?>("currency", currency));
            
        // Histogram: distribution of payment processing latency
        var start = Stopwatch.GetTimestamp();
        await ChargeAsync(amount, currency);
        var elapsed = Stopwatch.GetElapsedTime(start);
        
        metrics.RecordHistogram("payments.duration_ms", elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>(TelemetryTags.Outcome, "success"));
    }
}

// Example: Gauge for queue depth (polled periodically)
public class QueueMonitor(IMetricsCollector metrics, IMessageQueue queue)
{
    public void ReportQueueDepth()
    {
        var depth = queue.GetApproximateDepth();
        metrics.RecordGauge("queue.depth", depth,
            new KeyValuePair<string, object?>("queue.name", "payments"));
    }
}
```

### Query Examples (Prometheus/Grafana)

```promql
# Request rate per Node
rate(payments_processed_total{hd_node_id="payment-node"}[5m])

# P95 latency
histogram_quantile(0.95, sum by (le) (rate(payments_duration_ms_bucket[5m])))

# Average payment amount by currency
avg(payments_amount) by (currency)

# Current queue depth
queue_depth{queue_name="payments"}
```

[↑ Back to top](#table-of-contents)

---

## NoOpMetricsCollector (Implementation)

**What it is:** Default implementation that discards all metrics with minimal overhead.

**Location:** `HoneyDrunk.Kernel/Diagnostics/NoOpMetricsCollector.cs`

**Design:** Kernel hosting registers `NoOpMetricsCollector` by default so Nodes can depend on `IMetricsCollector` without configuring a metrics backend. **Production Nodes should replace this** with a concrete implementation (for example, an OpenTelemetry-backed collector).

### Registration

```csharp
// Default (Kernel): NoOpMetricsCollector is registered automatically
// No explicit registration needed

// Production (Node): replace with a real implementation
builder.Services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();
```

### When to Replace

| Environment | Recommendation |
|-------------|----------------|
| **Development / Local** | `NoOpMetricsCollector` is fine (minimal overhead) |
| **Staging** | Real implementation for validation |
| **Production** | **Always** use a real implementation (OpenTelemetry, Prometheus, Application Insights) |

[↑ Back to top](#table-of-contents)

---

## Relationship to Telemetry

**Diagnostics and Telemetry are complementary observability primitives:**

| Aspect | Telemetry (Traces + Logs) | Diagnostics (Metrics) |
|--------|---------------------------|----------------------|
| **Question** | What happened? | How much? How often? How long? |
| **Example** | "Payment processed for order-123 in 150ms" | "95th percentile latency: 120ms" |
| **Cardinality** | High (unique trace per request) | Low (aggregated over time/dimensions) |
| **Storage** | Sampled (e.g., 10% of traces) | Continuous (all metrics) |
| **Use Case** | Debugging, root cause analysis | Alerting, dashboards, trends |
| **Standard Tags** | `TelemetryTags` for correlation | Reuse `TelemetryTags` where possible |

**Design Philosophy:**
- **Traces** tell you the story of a single request/operation
- **Metrics** tell you the aggregate behavior over time
- **Both** use `TelemetryTags` for correlation (e.g., filter by `hd.node_id`, `hd.environment`)

### OpenTelemetry Integration

A typical implementation will adapt `IMetricsCollector` to **OpenTelemetry metrics** (`Meter`, `Counter`, `Histogram`, `ObservableGauge`) and export via **OTLP** alongside traces.

**Example Integration:**

```csharp
public class OpenTelemetryMetricsCollector(Meter meter) : IMetricsCollector
{
    private readonly Counter<long> _counter = meter.CreateCounter<long>("metrics");
    private readonly Histogram<double> _histogram = meter.CreateHistogram<double>("metrics");
    
    public void RecordCounter(string name, long value, params KeyValuePair<string, object?>[] tags)
    {
        _counter.Add(value, tags.Prepend(new KeyValuePair<string, object?>("metric.name", name)).ToArray());
    }
    
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        _histogram.Record(value, tags.Prepend(new KeyValuePair<string, object?>("metric.name", name)).ToArray());
    }
    
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        // ObservableGauge requires callback pattern - implementation varies
    }
}

// Registration
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("YourApp.*");
        metrics.AddOtlpExporter();
    });

builder.Services.AddSingleton<IMetricsCollector>(sp =>
{
    var meterFactory = sp.GetRequiredService<IMeterFactory>();
    var meter = meterFactory.Create("YourApp.Metrics");
    return new OpenTelemetryMetricsCollector(meter);
});
```

[↑ Back to top](#table-of-contents)

---

## Summary

**Diagnostics provides minimal abstractions for recording metrics.**

| Component | Purpose | Type | Audience |
|-----------|---------|------|----------|
| **`IMetricsCollector`** | Record counters, histograms, gauges | Interface | All |
| **`NoOpMetricsCollector`** | Default no-op implementation | Implementation | Development |

**Key Patterns:**
- Use **Counter** for event counts (requests, errors)
- Use **Histogram** for value distributions (latency, amounts)
- Use **Gauge** for point-in-time measurements (queue depth, memory)
- **Reuse `TelemetryTags`** for correlation with traces and logs
- **Thread-safe implementations required**
- Replace `NoOpMetricsCollector` in production

**Observability Trinity:**
- **Telemetry** - Traces + Logs (what happened, detailed story)
- **Diagnostics** - Metrics (aggregate behavior over time)
- **Health** - Service status (working / impaired / failed)

The three work together for complete Grid observability.

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

