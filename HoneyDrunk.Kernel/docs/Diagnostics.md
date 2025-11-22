# 📈 Diagnostics - Metrics and Observability

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

Abstractions for recording application metrics without coupling to specific telemetry systems.

**Location:** `HoneyDrunk.Kernel.Abstractions/Diagnostics/`

---

## IMetricsCollector.cs

```csharp
public interface IMetricsCollector
{
    void RecordCounter(string name, long value, params KeyValuePair<string, object?>[] tags);
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);
}
```

### Usage Example

```csharp
public class PaymentProcessor(IMetricsCollector metrics)
{
    public async Task ProcessAsync(decimal amount, string currency)
    {
        metrics.RecordCounter("payments.processed", 1,
            new KeyValuePair<string, object?>("currency", currency));
            
        metrics.RecordHistogram("payments.amount", (double)amount,
            new KeyValuePair<string, object?>("currency", currency));
            
        var start = Stopwatch.GetTimestamp();
        await ChargeAsync(amount, currency);
        var elapsed = Stopwatch.GetElapsedTime(start);
        
        metrics.RecordHistogram("payments.duration_ms", elapsed.TotalMilliseconds);
    }
}
```

---

## NoOpMetricsCollector (Implementation)

**Location:** `HoneyDrunk.Kernel/Diagnostics/NoOpMetricsCollector.cs`

Default implementation that discards all metrics (zero overhead).

```csharp
// Registered by default
services.AddHoneyDrunkCore(...);  // Uses NoOpMetricsCollector

// Replace in production
services.AddSingleton<IMetricsCollector, OpenTelemetryCollector>();
```

---

[← Back to File Guide](FILE_GUIDE.md)

