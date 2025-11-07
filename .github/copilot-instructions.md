# HoneyDrunk.Kernel Repository Guidelines

## Project Overview

This repository contains the foundational runtime for **HoneyDrunk.OS** (“the Hive”).  
It defines:
- Contracts and primitives (DI, configuration, diagnostics, context, time/IDs, health)
- Lightweight runtime implementations
- Repo-wide standards wiring (via **HoneyDrunk.Standards**)
- CI packaging for internal feeds

This is a **.NET 10.0** solution composed of:
- `HoneyDrunk.Kernel.Abstractions` (contracts-only)
- `HoneyDrunk.Kernel` (runtime primitives)
- `HoneyDrunk.Kernel.Tests` (separate test project)

---

## Technology Stack

- **Framework:** .NET 10.0  
- **Language:** C#  
- **Project Types:** Class Libraries (+ xUnit test project)  
- **Features Enabled:**  
  - Implicit Usings  
  - Nullable Reference Types  
  - Primary Constructors  

---

## Coding Standards

### C# Conventions

- Follow Microsoft C# conventions plus **HoneyDrunk.Standards** analyzers (buildTransitive).  
- Nullable enabled everywhere; avoid `!` suppression unless justified.  
- Favor **primary constructors** for concise, immutable design.  
- **PascalCase** for public types/members; **camelCase** for locals/parameters.  
- Private fields only when state is required; prefer constructor-injected, readonly dependencies.  
- Keep interfaces minimal and composable; avoid “god” interfaces.

### Code Organization

- **No `/src` or `/tests` folders.** Projects live at repo root:  
  - `HoneyDrunk.Kernel.Abstractions/`  
  - `HoneyDrunk.Kernel/`  
  - `HoneyDrunk.Kernel.Tests/`
- Place repo-level configuration files at root (`.editorconfig`, `.gitattributes`, `Directory.Build.props`, `Directory.Build.targets`, `NuGet.config`).
- Keep implementations thin; heavy behavior belongs in downstream nodes (Transport, Data, Web.Rest).

### Documentation

- XML docs required for all public APIs in `Abstractions`.  
- `README.md` must reflect current responsibilities, layout, and build instructions.  
- Update documentation when changing or extending public contracts.

---

## Build and Testing

### Building the Solution

```bash
dotnet restore
dotnet build -c Release
```

- Targets **.NET 10.0**.
- Warnings are treated as errors (enforced via props/Standards).

### Testing

```bash
dotnet test HoneyDrunk.Kernel.Tests/HoneyDrunk.Kernel.Tests.csproj -c Release --no-build
```

- Tests live only in `HoneyDrunk.Kernel.Tests` (no test code in runtime libraries).  
- Use Kernel abstractions (`IClock`, `IIdGenerator`) for deterministic behavior.  
- Prefer **xUnit** + **FluentAssertions**.  
- Keep tests fast, isolated, and repeatable.

---

## File Management

### Ignore Patterns

- Respect `.gitignore` (no `bin/`, `obj/`, user files, or secrets).  
- Never commit environment files or tokens.

### File Naming

- C# files: **PascalCase** matching the public type.  
- Config and YAML: clear, purpose-driven names.

---

## Contribution Guidelines

### Making Changes

- Keep PRs small and focused.  
- Avoid breaking public contracts in `Abstractions` without discussion.  
- Update docs and samples with any code changes.  
- Run build and test locally before pushing.

### Code Reviews

- All changes require review.  
- Analyzer compliance via **HoneyDrunk.Standards** is mandatory.  
- Verify new primitives belong in **Kernel**, not downstream nodes.

### Commit Messages

- Use conventional commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`.  
- Use present tense, concise first lines (<50 chars).  

---

## Special Considerations

### Kernel Philosophy

- **Make decisions once.** Kernel is the grammar all Nodes speak.  
- **Small surface, strong contracts.** Prefer stable interfaces over frameworks.  
- **Observability-ready.** Expose hooks; avoid hard dependencies on specific telemetry stacks.  
- **Security-first.** Prepare for Vault integration; never hardcode secrets.

### Compatibility

- Support all modern IDEs: Visual Studio, VS Code, Rider.  
- Cross-platform (Windows, macOS, Linux) by default.

---

## CI/CD (GitHub Actions)

- **Triggers:** `push`, `pull_request`, and `tags` (`v*`)  
- **Steps:** build → test → pack → publish  
- **Secrets:**  
  - `HD_FEED_URL`  
  - `HD_FEED_USER`  
  - `HD_FEED_TOKEN`  
- Required checks on `main`: **Build**, **Test**  
- Output packages:  
  - `HoneyDrunk.Kernel.Abstractions`  
  - `HoneyDrunk.Kernel`
