using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions;

/// <summary>
/// Well-known infrastructure Node identifiers required by Kernel and core Grid operations.
/// </summary>
/// <remarks>
/// <para>
/// This registry contains ONLY Nodes that are critical to Grid infrastructure.
/// Application-level Nodes (Market apps, creator tools, etc.) belong in:
/// - HoneyDrunk.Grid.Contracts (Grid-wide catalog, code-generated from nodes.json).
/// - Per-application constants (e.g., Arcadia defines its own NodeId).
/// </para>
/// <para>
/// <strong>Inclusion Criteria:</strong>
/// "If I deleted this Node, would basic Grid operations (tracing, secrets, persistence, auth, observability) break?".
/// </para>
/// <para>
/// <strong>Architecture:</strong>
/// Kernel provides primitives (NodeId, context, lifecycle).
/// Grid provides the catalog (nodes.json ? GridNodes).
/// Applications use both but define their own identities.
/// </para>
/// </remarks>
public static class WellKnownNodes
{
    /// <summary>
    /// Core infrastructure services - foundational primitives for all Nodes.
    /// </summary>
    public static class Core
    {
        /// <summary>Semantic OS layer - context propagation, lifecycle orchestration, Grid primitives.</summary>
        public static readonly NodeId Kernel = new("HoneyDrunk.Kernel");

        /// <summary>Outbox, messaging abstractions, and reliable transport layer.</summary>
        public static readonly NodeId Transport = new("HoneyDrunk.Transport");

        /// <summary>Secrets and configuration manager - zero-trust vault for sensitive data.</summary>
        public static readonly NodeId Vault = new("HoneyDrunk.Vault");

        /// <summary>Persistence conventions, migrations, tenant isolation, and outbox integration.</summary>
        public static readonly NodeId Data = new("HoneyDrunk.Data");

        /// <summary>REST scaffolding and conventions - clean, consistent HTTP interfaces.</summary>
        public static readonly NodeId WebRest = new("HoneyCore.Web.Rest");

        /// <summary>Unified authentication and authorization with passkeys, JWT, MFA, and policy.</summary>
        public static readonly NodeId Auth = new("HoneyDrunk.Auth");
    }

    /// <summary>
    /// Observability and orchestration - Grid-wide coordination services.
    /// </summary>
    public static class Ops
    {
        /// <summary>Observability suite - logs, traces, and metrics in one interface.</summary>
        public static readonly NodeId Pulse = new("Pulse");

        /// <summary>Service registry and node orchestration - live registry of Nodes and dependencies.</summary>
        public static readonly NodeId Grid = new("HoneyDrunk.Grid");
    }
}
