namespace HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Represents root configuration for HoneyDrunk Grid participation (studio + environment scope).
/// </summary>
/// <remarks>
/// Node bootstrap binds this along with <c>HoneyDrunkNodeOptions</c> to establish identity and telemetry conventions.
/// </remarks>
public sealed class HoneyDrunkGridOptions
{
    /// <summary>
    /// Gets or sets the Studio identifier (multi-studio isolation boundary). Required.
    /// </summary>
    public string StudioId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Environment identifier (e.g. production, staging, dev-alice). Required.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional cluster / deployment group hint.
    /// </summary>
    public string? Cluster { get; set; }

    /// <summary>
    /// Gets or sets an optional deployment slot (blue, green, canary) for rollout strategies.
    /// </summary>
    public string? Slot { get; set; }

    /// <summary>
    /// Gets a set of arbitrary low-cardinality tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; } = [];

    /// <summary>
    /// Basic validation of required fields.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(StudioId))
        {
            throw new InvalidOperationException("HoneyDrunkGridOptions.StudioId is required.");
        }

        if (string.IsNullOrWhiteSpace(Environment))
        {
            throw new InvalidOperationException("HoneyDrunkGridOptions.Environment is required.");
        }
    }
}
