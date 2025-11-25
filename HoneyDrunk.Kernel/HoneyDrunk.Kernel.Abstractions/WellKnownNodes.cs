using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions;

/// <summary>
/// Well-known Node identifiers for infrastructure components required by Kernel.
/// </summary>
/// <remarks>
/// Contains only core infrastructure nodes. Application nodes should define their own NodeIds.
/// All identifiers use kebab-case for consistency.
/// </remarks>
public static class WellKnownNodes
{
    /// <summary>
    /// Core infrastructure nodes - foundational primitives for the Grid.
    /// </summary>
    public static class Core
    {
        /// <summary>Semantic OS layer - context propagation, lifecycle orchestration.</summary>
        public static readonly NodeId Kernel = new("kernel");

        /// <summary>Outbox, messaging abstractions, and reliable transport layer.</summary>
        public static readonly NodeId Transport = new("transport");

        /// <summary>Secrets and configuration manager - zero-trust vault.</summary>
        public static readonly NodeId Vault = new("vault");

        /// <summary>Persistence conventions, migrations, tenant isolation.</summary>
        public static readonly NodeId Data = new("data");

        /// <summary>REST scaffolding and conventions.</summary>
        public static readonly NodeId WebRest = new("web-rest");

        /// <summary>Unified authentication and authorization.</summary>
        public static readonly NodeId Auth = new("auth");
    }

    /// <summary>
    /// Observability and orchestration nodes.
    /// </summary>
    public static class Ops
    {
        /// <summary>Observability suite - logs, traces, and metrics.</summary>
        public static readonly NodeId Pulse = new("pulse");

        /// <summary>Service registry and node orchestration.</summary>
        public static readonly NodeId Grid = new("grid");
    }
}
