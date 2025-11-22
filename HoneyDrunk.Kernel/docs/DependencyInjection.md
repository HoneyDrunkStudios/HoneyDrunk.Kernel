# 🔌 Dependency Injection - Modular Service Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

Modular service registration through self-contained modules.

**Location:** `HoneyDrunk.Kernel.Abstractions/DI/`

---

## IModule.cs

```csharp
public interface IModule
{
    void ConfigureServices(IServiceCollection services);
}
```

### Usage Example

```csharp
public class TransportModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITransportPublisher, ServiceBusPublisher>();
        services.AddSingleton<ITransportConsumer, ServiceBusConsumer>();
        services.AddHostedService<MessageConsumerHostedService>();
    }
}

// In Program.cs
var transportModule = new TransportModule();
transportModule.ConfigureServices(builder.Services);
```

---

[← Back to File Guide](../FILE_GUIDE_NEW.md)

