using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions;

/// <summary>
/// Well-known HoneyDrunk Grid Node identifiers.
/// </summary>
/// <remarks>
/// These identifiers mirror the canonical HoneyDrunk Architecture node catalog and
/// the deploy-time <c>HONEYDRUNK_NODE_ID</c> values used by Grid services.
/// All identifiers use kebab-case and the <c>honeydrunk-</c> prefix.
/// </remarks>
public static class WellKnownNodes
{
    /// <summary>
    /// Core-sector nodes - foundational primitives for the Grid.
    /// </summary>
    public static class Core
    {
        /// <summary>Semantic OS layer - context propagation, lifecycle orchestration.</summary>
        public static readonly NodeId Kernel = new("honeydrunk-kernel");

        /// <summary>Outbox, messaging abstractions, and reliable transport layer.</summary>
        public static readonly NodeId Transport = new("honeydrunk-transport");

        /// <summary>Secrets and configuration manager - zero-trust vault.</summary>
        public static readonly NodeId Vault = new("honeydrunk-vault");

        /// <summary>Secret rotation jobs and third-party credential refresh orchestration.</summary>
        public static readonly NodeId VaultRotation = new("honeydrunk-vault-rotation");

        /// <summary>Unified authentication and authorization.</summary>
        public static readonly NodeId Auth = new("honeydrunk-auth");

        /// <summary>REST scaffolding and conventions.</summary>
        public static readonly NodeId WebRest = new("honeydrunk-web-rest");

        /// <summary>Persistence conventions, migrations, tenant isolation.</summary>
        public static readonly NodeId Data = new("honeydrunk-data");

        /// <summary>Durable, attributable audit substrate.</summary>
        public static readonly NodeId Audit = new("honeydrunk-audit");
    }

    /// <summary>
    /// Ops-sector nodes - operations, observability, communications, and delivery.
    /// </summary>
    public static class Ops
    {
        /// <summary>Observability suite - logs, traces, and metrics.</summary>
        public static readonly NodeId Pulse = new("honeydrunk-pulse");

        /// <summary>Multi-channel communication orchestration and decision logging.</summary>
        public static readonly NodeId Communications = new("honeydrunk-communications");

        /// <summary>Notification delivery and provider fan-out.</summary>
        public static readonly NodeId Notify = new("honeydrunk-notify");

        /// <summary>Grid CI/CD control plane and reusable workflow ownership.</summary>
        public static readonly NodeId Actions = new("honeydrunk-actions");
    }

    /// <summary>
    /// Meta-sector nodes - architecture, public surfaces, and knowledge systems.
    /// </summary>
    public static class Meta
    {
        /// <summary>Grid command center for ADRs, catalogs, packets, and invariants.</summary>
        public static readonly NodeId Architecture = new("honeydrunk-architecture");

        /// <summary>HoneyDrunk Studios public website and storytelling surface.</summary>
        public static readonly NodeId Studios = new("honeydrunk-studios");

        /// <summary>Living knowledge surface and LLM-compiled wiki.</summary>
        public static readonly NodeId Lore = new("honeydrunk-lore");
    }

    /// <summary>
    /// AI-sector nodes - agentic runtime, memory, knowledge, evaluation, and control.
    /// </summary>
    public static class AI
    {
        /// <summary>Model abstractions, routing contracts, and provider composition.</summary>
        public static readonly NodeId Ai = new("honeydrunk-ai");

        /// <summary>Tool/capability registry and invocation guardrails.</summary>
        public static readonly NodeId Capabilities = new("honeydrunk-capabilities");

        /// <summary>Agent runtime contracts and orchestration primitives.</summary>
        public static readonly NodeId Agents = new("honeydrunk-agents");

        /// <summary>Long-term memory scoped by tenant, project, and agent.</summary>
        public static readonly NodeId Memory = new("honeydrunk-memory");

        /// <summary>Knowledge graph and retrieval substrate.</summary>
        public static readonly NodeId Knowledge = new("honeydrunk-knowledge");

        /// <summary>Durable workflow coordination for agentic processes.</summary>
        public static readonly NodeId Flow = new("honeydrunk-flow");

        /// <summary>Human oversight, approvals, circuit breakers, and policy control.</summary>
        public static readonly NodeId Operator = new("honeydrunk-operator");

        /// <summary>Evaluation harnesses and quality gates for agentic work.</summary>
        public static readonly NodeId Evals = new("honeydrunk-evals");

        /// <summary>Simulation and projection surface for planning and risk assessment.</summary>
        public static readonly NodeId Sim = new("honeydrunk-sim");
    }
}
