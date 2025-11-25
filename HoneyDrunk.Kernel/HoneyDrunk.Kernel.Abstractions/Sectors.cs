using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions;

/// <summary>
/// Well-known Sector identifiers for Grid organization.
/// </summary>
/// <remarks>
/// Sectors group related Nodes by functional domain. Use these static values
/// for consistency across the Grid. These map to the canonical sectors defined
/// in the HoneyDrunk Grid schema.
/// </remarks>
public static class Sectors
{
    /// <summary>
    /// Core infrastructure services - foundational primitives for the Grid.
    /// Kernel abstractions, data conventions, and reliable transport.
    /// </summary>
    public static readonly SectorId Core = SectorId.WellKnown.Core;

    /// <summary>
    /// Operations and monitoring services - CI/CD, deployments, and observability.
    /// From commit to production with confidence.
    /// </summary>
    public static readonly SectorId Ops = SectorId.WellKnown.Ops;

    /// <summary>
    /// AI and machine learning services - agents, orchestration, and cognition primitives.
    /// Lifecycles, memory, orchestration, and safety.
    /// </summary>
    public static readonly SectorId AI = SectorId.WellKnown.AI;

    /// <summary>
    /// Creator tools and platforms - content intelligence and amplification.
    /// Tools that turn imagination into momentum.
    /// </summary>
    public static readonly SectorId Creator = SectorId.WellKnown.Creator;

    /// <summary>
    /// Market-facing applications - public SaaS and consumer products.
    /// Applied innovation for the open world.
    /// </summary>
    public static readonly SectorId Market = SectorId.WellKnown.Market;

    /// <summary>
    /// Gaming and media services - worlds, leagues, and narrative experiences.
    /// Gaming, narrative, and media where technology becomes emotion.
    /// </summary>
    public static readonly SectorId HoneyPlay = SectorId.WellKnown.HoneyPlay;

    /// <summary>
    /// Robotics and hardware services - simulation, servos, and embodied agents.
    /// Where physical motion meets digital logic.
    /// </summary>
    public static readonly SectorId Cyberware = SectorId.WellKnown.Cyberware;

    /// <summary>
    /// Security and defense services - breach simulations and secure-by-default SDKs.
    /// Proactive defense for the Hive.
    /// </summary>
    public static readonly SectorId HoneyNet = SectorId.WellKnown.HoneyNet;

    /// <summary>
    /// Meta services - registries, documentation, and knowledge systems.
    /// The ecosystem's self-awareness.
    /// </summary>
    public static readonly SectorId Meta = SectorId.WellKnown.Meta;
}
