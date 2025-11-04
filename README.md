# HoneyDrunk.Kernel

Foundational primitives for HoneyDrunk.OS

## 🧬 Overview

HoneyDrunk.Kernel is the primordial layer of the Grid — the bedrock that powers every Node, service, and agent across HoneyDrunk.OS.
It defines the shared primitives that make the ecosystem coherent and interoperable: dependency injection, configuration, diagnostics, context propagation, and application lifecycles.
Every architectural pattern within the Grid ultimately descends from the Kernel.

**Signal Quote:** "Where everything begins."

## 🚀 Purpose

The Kernel exists to make architectural decisions once, not repeatedly across services.
It's how small teams achieve large-scale stability — one unified runtime grammar guiding the entire Hive.

## 🧩 Core Responsibilities

| Area | Description |
|------|-------------|
| Dependency Injection (DI) | Central composition layer for service registration and lifetime scoping. |
| Configuration | Unified configuration provider that reads from environment variables, manifests, and Vault. |
| Context Propagation | Lightweight context object for tracing, correlation, and cancellation across async boundaries. |
| Diagnostics | Shared contracts for logging, metrics, and health checks. |
| Time & ID Abstractions | Deterministic abstractions for time and unique identifiers to improve testability. |
| Hosting Lifecycle | Common startup, shutdown, and background worker orchestration primitives. |

## 🧠 Design Philosophy

- **Predictability > Cleverness** – Simplicity scales.
- **Replaceable without regret** – Kernel defines contracts, not frameworks.
- **Observable by default** – Every operation should emit measurable signals.
- **Secure by design** – Vault integration from the start, not bolted on later.
- **Portable** – Works in APIs, background services, or agent runtimes.

## 🧱 Repository Layout

```
HoneyDrunk.Kernel/
 ├── HoneyDrunk.Kernel/                 # Runtime library
 ├── HoneyDrunk.Kernel.Abstractions/    # Interfaces & shared contracts
 ├── HoneyDrunk.Kernel.Tests/           # Separate test project
 ├── HoneyDrunk.Kernel.sln
 ├── Directory.Build.props
 ├── Directory.Build.targets
 ├── .editorconfig
 ├── .gitattributes
 ├── .gitignore
 ├── CODEOWNERS
 └── .github/
     └── workflows/
         └── build.yml
```

### Testing Policy

- All tests live in `HoneyDrunk.Kernel.Tests` — none in runtime projects.
- Shared fixtures will later come from `HoneyDrunk.Testing`.
- Tests must use `IClock` and `IIdGenerator` for deterministic runs.
- CI gate: build fails if tests fail; coverage threshold optional.

## 🔗 Relationships

**Upstream:**
- HoneyDrunk.Standards
- HoneyDrunk.Build

**Downstream:**
- HoneyDrunk.Data
- HoneyDrunk.Transport
- HoneyCore.Web.Rest
- HoneyDrunk.Auth
- HoneyDrunk.Vault

## 🧪 Local Development

```bash
git clone https://github.com/HoneyDrunkStudios/kernel
cd kernel

dotnet restore
dotnet build
dotnet test HoneyDrunk.Kernel.Tests/HoneyDrunk.Kernel.Tests.csproj
```

This Node consumes private packages from the HoneyDrunk Azure Artifacts feed.
Configure the following secrets or NuGet.config sources:

- `HD_FEED_URL`
- `HD_FEED_USER`
- `HD_FEED_TOKEN`

## ⚙️ Build & Release

- **Workflow:** `HoneyDrunk.Actions` → `publish-nuget.yml`
- **Tag Convention:** `vX.Y.Z` → triggers build, pack, and publish
- **Analyzers:** Enforced automatically via `HoneyDrunk.Standards` (buildTransitive)
- **Output:** Internal Azure Artifacts feed

CI runs on:
- `push` → build + test
- `pull_request` → validate formatting and analyzers
- `tag v*` → publish package

## 🧃 Motto

**"If the Kernel is stable, everything above it can change fearlessly."**
