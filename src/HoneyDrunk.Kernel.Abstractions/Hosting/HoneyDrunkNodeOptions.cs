using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Options supplied when converting a plain application into a HoneyDrunk Node via bootstrap extension.
/// </summary>
/// <remarks>
/// Defines minimal identity and telemetry behavior. Downstream packages extend via separate option types.
/// </remarks>
public sealed class HoneyDrunkNodeOptions
{
    /// <summary>
    /// Gets or sets the strongly-typed Node identifier (required).
    /// </summary>
    public NodeId? NodeId { get; set; }

    /// <summary>
    /// Gets or sets the sector identifier for logical grouping (required).
    /// </summary>
    public SectorId? SectorId { get; set; }

    /// <summary>
    /// Gets or sets the Studio identifier owning this Node (required).
    /// </summary>
    public string StudioId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Environment identifier (required).
    /// </summary>
    public EnvironmentId? EnvironmentId { get; set; }

    /// <summary>
    /// Gets or sets the multi-tenancy execution mode.
    /// </summary>
    public MultiTenancyMode TenancyMode { get; set; } = MultiTenancyMode.SingleTenant;

    /// <summary>
    /// Gets or sets the semantic version override (defaults to assembly version).
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets telemetry configuration sub-options.
    /// </summary>
    public HoneyDrunkTelemetryOptions Telemetry { get; set; } = new();

    /// <summary>
    /// Gets low-cardinality discovery / filtering tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; } = [];

    /// <summary>
    /// Validates required invariants.
    /// </summary>
    public void Validate()
    {
        if (NodeId is null)
        {
            throw new InvalidOperationException("NodeId is required.");
        }

        if (SectorId is null)
        {
            throw new InvalidOperationException("SectorId is required.");
        }

        if (EnvironmentId is null)
        {
            throw new InvalidOperationException("EnvironmentId is required.");
        }

        if (string.IsNullOrWhiteSpace(StudioId))
        {
            throw new InvalidOperationException("StudioId is required.");
        }
    }
}
