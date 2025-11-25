# 🔌 Dependency Injection - Modular Service Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IModule.cs](#imodulecs)

---

## Overview

Modular service registration through self-contained modules.

**Location:** `HoneyDrunk.Kernel.Abstractions/DI/`

[↑ Back to top](#table-of-contents)

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

[↑ Back to top](#table-of-contents)

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

