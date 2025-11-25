using System.Text.RegularExpressions;

namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Sector identifier that groups related Nodes logically within the Grid.
/// </summary>
/// <remarks>
/// A <see cref="SectorId"/> is a low-cardinality classification used for coarse grouping (e.g., <c>core</c>, <c>ai</c>, <c>ops</c>).
/// It is intentionally simple to avoid taxonomy proliferation. The value must be kebab-case (lowercase letters and digits
/// separated by single hyphens) with a length between 2 and 32 characters.
/// Examples: <c>core</c>, <c>ai</c>, <c>ops</c>, <c>creator</c>, <c>market</c>.
/// </remarks>
public readonly partial record struct SectorId
{
    private static readonly Regex ValidationPattern = ValidationRegex();
    private const int MinLength = 2;
    private const int MaxLength = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectorId"/> struct.
    /// </summary>
    /// <param name="value">The sector identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
    public SectorId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (!IsValid(value, out var error))
        {
            throw new ArgumentException(error, nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying string value of the sector identifier.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    /// Implicit conversion from <see cref="SectorId"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="sectorId">The sector identifier to convert.</param>
    public static implicit operator string(SectorId sectorId) => sectorId.Value;

    /// <summary>
    /// Validates a string as a potential <see cref="SectorId"/> value.
    /// </summary>
    /// <param name="value">The candidate value.</param>
    /// <param name="errorMessage">Populated with a descriptive error when validation fails.</param>
    /// <returns><c>true</c> if valid; otherwise <c>false</c>.</returns>
    public static bool IsValid(string? value, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Sector ID cannot be null or whitespace.";
            return false;
        }

        if (value.Length < MinLength || value.Length > MaxLength)
        {
            errorMessage = $"Sector ID must be between {MinLength} and {MaxLength} characters.";
            return false;
        }

        if (!ValidationPattern.IsMatch(value))
        {
            errorMessage = "Sector ID must be kebab-case: lowercase letters, digits, and single hyphens only.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="SectorId"/>.
    /// </summary>
    /// <param name="value">The candidate value.</param>
    /// <param name="sectorId">The parsed <see cref="SectorId"/> when successful.</param>
    /// <returns><c>true</c> if parsing succeeds; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? value, out SectorId sectorId)
    {
        if (IsValid(value, out _))
        {
            sectorId = new SectorId(value!);
            return true;
        }

        sectorId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    /// <summary>
    /// Well-known sector identifiers for the HoneyDrunk Grid.
    /// </summary>
    public static class WellKnown
    {
        /// <summary>Core infrastructure services - foundational primitives for the Grid.</summary>
        /// <remarks>Kernel abstractions, data conventions, and reliable transport.</remarks>
        public static readonly SectorId Core = new("core");

        /// <summary>Operations and monitoring services - CI/CD, deployments, and observability.</summary>
        /// <remarks>From commit to production with confidence.</remarks>
        public static readonly SectorId Ops = new("ops");

        /// <summary>AI and machine learning services - agents, orchestration, and cognition primitives.</summary>
        /// <remarks>Lifecycles, memory, orchestration, and safety.</remarks>
        public static readonly SectorId AI = new("ai");

        /// <summary>Creator tools and platforms - content intelligence and amplification.</summary>
        /// <remarks>Tools that turn imagination into momentum.</remarks>
        public static readonly SectorId Creator = new("creator");

        /// <summary>Market-facing applications - public SaaS and consumer products.</summary>
        /// <remarks>Applied innovation for the open world.</remarks>
        public static readonly SectorId Market = new("market");

        /// <summary>Gaming and media services - worlds, leagues, and narrative experiences.</summary>
        /// <remarks>Gaming, narrative, and media where technology becomes emotion.</remarks>
        public static readonly SectorId HoneyPlay = new("honeyplay");

        /// <summary>Robotics and hardware services - simulation, servos, and embodied agents.</summary>
        /// <remarks>Where physical motion meets digital logic.</remarks>
        public static readonly SectorId Cyberware = new("cyberware");

        /// <summary>Security and defense services - breach simulations and secure-by-default SDKs.</summary>
        /// <remarks>Proactive defense for the Hive.</remarks>
        public static readonly SectorId HoneyNet = new("honeynet");

        /// <summary>Meta services - registries, documentation, and knowledge systems.</summary>
        /// <remarks>The ecosystem's self-awareness.</remarks>
        public static readonly SectorId Meta = new("meta");
    }
}
