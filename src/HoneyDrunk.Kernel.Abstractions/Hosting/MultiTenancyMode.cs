namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Multi-tenancy execution mode for a Node.
/// </summary>
public enum MultiTenancyMode
{
    /// <summary>
    /// Single tenant execution; all requests implicitly scoped to one Studio/Tenant.
    /// </summary>
    SingleTenant,

    /// <summary>
    /// Explicit tenant resolution per request (header / token based).
    /// </summary>
    PerRequest,

    /// <summary>
    /// Segmented by project/workspace inside a tenant (Studio + Project resolution).
    /// </summary>
    ProjectSegmented,
}
