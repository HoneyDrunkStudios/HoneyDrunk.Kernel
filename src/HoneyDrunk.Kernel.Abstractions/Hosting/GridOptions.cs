namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Configuration options for Grid-level hosting.
/// </summary>
public sealed class GridOptions
{
    /// <summary>
    /// Gets or sets the Node identifier.
    /// Example: "payment-node", "notification-node".
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Node version.
    /// Example: "1.0.0", "2.1.3-beta".
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the Studio identifier.
    /// Example: "honeycomb", "staging".
    /// </summary>
    public string StudioId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the environment name.
    /// Example: "production", "staging", "development".
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Gets optional tags/labels for this Node instance.
    /// </summary>
    public Dictionary<string, string> Tags { get; } = [];
}
