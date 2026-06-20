# Changelog

This file summarizes notable changes across the HoneyDrunk.Kernel repository at a
release-level. The authoritative, detailed per-package history lives in the
per-package changelogs:

- [HoneyDrunk.Kernel.Abstractions CHANGELOG](HoneyDrunk.Kernel/HoneyDrunk.Kernel.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Kernel CHANGELOG](HoneyDrunk.Kernel/HoneyDrunk.Kernel/CHANGELOG.md)

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.8.0] - 2026-05-26

### Changed

- Converted `AgentResultSerializer`, `GridContextSerializer`, `HttpContextMapper`, and `JobContextMapper` from sealed classes to static classes; dropped `JobContextMapper`'s unused instance constructor and fields (breaking).
- Reordered the `GridContextSnapshot` constructor so `CancellationToken cancellation` is the last parameter, matching .NET conventions (breaking for positional callers).
- Bumped `HoneyDrunk.Kernel` and `HoneyDrunk.Kernel.Abstractions` to `0.8.0` per pre-1.0 semantic versioning.
- Drove the remaining ADR-0011 maintainability findings on the SonarCloud dashboard to zero, preferring real code fixes over suppressions.

## [0.7.0] - 2026-05-18

### Added

- Added `GridContextSnapshot` in Abstractions so downstream libraries can create initialized context snapshots without referencing the Kernel runtime package.

### Changed

- Consolidated duplicated HTTP header, correlation, and baggage extraction logic shared by `HttpContextMapper` and `GridContextMiddleware`.
- Consolidated repeated ULID and kebab-case identity parsing and validation helpers across Kernel identity types.

## [0.6.0] - 2026-05-17

### Added

- Added missing well-known Node IDs for Vault.Rotation, Audit, Communications, Notify, Actions, Architecture, Studios, Lore, and the current AI-sector Nodes.
- Added tests pinning every well-known Node ID to its canonical Grid identity and verifying uniqueness and validity.

### Changed

- `WellKnownNodes` now exposes canonical `honeydrunk-*` Node IDs instead of short local aliases such as `kernel`, `transport`, and `vault` (breaking).
- Migrated the test project from `xunit` v2 to `xunit.v3`.

## [0.5.0] - 2026-05-04

### Added

- Added `TenantId.Internal` and `TenantId.IsInternal` plus tenancy contracts (`ITenantRateLimitPolicy`, `TenantRateLimitDecision`, `TenantRateLimitOutcome`, `IBillingEventEmitter`, `BillingEvent`) and their default no-op runtime implementations.

### Changed

- Promoted tenant identity from string propagation to a first-class `TenantId` Grid primitive across context, mappers, middleware, transport binders, and serialization (ADR-0026, breaking).

## [0.4.0] - 2026-01-19

### Added

- See the per-package changelogs for `0.4.0` and earlier release history (back to the `0.1.0` Genesis release).
