namespace HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Defines the type/level of a configuration scope.
/// </summary>
public enum ConfigScopeType
{
    /// <summary>
    /// Global scope shared across all Studios and Nodes.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Studio-wide scope shared across all Nodes in a Studio.
    /// </summary>
    Studio = 1,

    /// <summary>
    /// Node-specific scope.
    /// </summary>
    Node = 2,

    /// <summary>
    /// Tenant-specific scope.
    /// </summary>
    Tenant = 3,

    /// <summary>
    /// Project-specific scope.
    /// </summary>
    Project = 4,

    /// <summary>
    /// Request/operation-specific scope.
    /// </summary>
    Request = 5,
}
