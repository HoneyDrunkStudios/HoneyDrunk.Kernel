# 🔐 Secrets - Secure Secrets Management

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [ISecretsSource.cs](#isecretssourcecs)
- [CompositeSecretsSource](#compositesecretssource-implementation)

---

## Overview

Secure access to passwords, API keys, and other sensitive data with fallback support.

**Location:** `HoneyDrunk.Kernel.Abstractions/Config/`

[↑ Back to top](#table-of-contents)

---

## ISecretsSource.cs

```csharp
public interface ISecretsSource
{
    bool TryGetSecret(string key, out string? value);
}
```

### Usage Example

```csharp
public class DatabaseConnector(ISecretsSource secrets)
{
    public string GetConnectionString()
    {
        if (secrets.TryGetSecret("DatabasePassword", out var password))
        {
            return $"Server=db;Password={password}";
        }
        throw new InvalidOperationException("Database password not found");
    }
}
```

[↑ Back to top](#table-of-contents)

---

## CompositeSecretsSource (Implementation)

**Location:** `HoneyDrunk.Kernel/Config/Secrets/CompositeSecretsSource.cs`

Chains multiple secret sources with fallback logic.

```csharp
var composite = new CompositeSecretsSource(new ISecretsSource[]
{
    new EnvironmentSecretsSource(),      // Try environment variables first
    new VaultSecretsSource(vaultClient),  // Then Vault
    new KeyVaultSource(keyVaultClient)    // Finally Azure Key Vault
});

if (composite.TryGetSecret("DatabasePassword", out var password))
{
    // Use password from first source that has it
}
```

[↑ Back to top](#table-of-contents)

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

