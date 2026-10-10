# HoneyDrunk.Kernel agent instructions

Own Grid/Node/Operation context, lifecycle and foundational contracts. Keep product rules and provider implementations in their owning nodes. Preserve async context propagation, trace/correlation separation and contracts-only dependency boundaries. Use the current source and package READMEs; do not copy old version numbers or assume every runtime identifier is a strongly typed ID.

Start with [README.md](README.md) and the relevant source/tests. Project files and lockfiles own SDK, framework and dependency versions.

Use the [file guide](HoneyDrunk.Kernel/docs/FILE_GUIDE.md) for context, lifecycle, configuration, telemetry and agent-interoperability details. Runtime context exposes string identifiers while optional identity primitives retain their own contracts; check the actual type before changing propagation or serialization.

Read the [shared engineering conventions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

## Verification

From the repository root for code/build changes:

```sh
dotnet restore HoneyDrunk.Kernel/HoneyDrunk.Kernel.slnx
dotnet build HoneyDrunk.Kernel/HoneyDrunk.Kernel.slnx -c Release --no-restore
dotnet test HoneyDrunk.Kernel/HoneyDrunk.Kernel.slnx -c Release --no-build
```

Use the checked-in workflow and relevant test documentation for additional integration prerequisites, coverage and consumer checks. Do not use live resources or credentials merely to make a local check pass.

## Code Review Rules

Apply the [shared review criteria](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md#code-review) to changed behavior, using the repository boundaries above. Report actionable findings with the failing path, concrete impact and a small corrective action; disclose unavailable evidence. These rules grant no cross-repository access or merge authority.

- Preserve Grid/Node/Operation context and foundational contracts without pulling product rules or provider implementations into Kernel. Check dependency direction and real public consumers before changing contracts or adding a shared abstraction.
- Trace scope and correlation through asynchronous work, nested operations, disposal and concurrent requests. Flag ambient-context leakage, dropped cancellation or confusion between trace and correlation identities; inspect hot-path allocations using actual callers.
- Require contract and lifecycle/context-isolation tests for changed behavior, including failure and concurrency paths. A catalog name is not proof of an implemented type; prefer the smallest change compatible with current packages.
