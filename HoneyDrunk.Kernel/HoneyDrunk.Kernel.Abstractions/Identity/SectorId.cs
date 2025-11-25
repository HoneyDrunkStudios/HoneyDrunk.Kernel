using System.Text.RegularExpressions;

namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Sector identifier that groups related Nodes logically within the Grid.
/// </summary>
/// <remarks>
/// A <see cref="SectorId"/> is a low-cardinality classification used for coarse grouping (e.g., <c>core</c>, <c>ai</c>, <c>ops</c>).
/// It is intentionally simple to avoid taxonomy proliferation. The value must be kebab-case (lowercase letters and digits
/// separated by single hyphens) with a length between 2 and 32 characters.
/// Examples: <c>core</c>, <c>ai</c>, <c>ops</c>, <c>data-services</c>.
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
    /// Well-known sector identifiers for common Grid sectors.
    /// </summary>
    public static class WellKnown
    {
        /// <summary>Core infrastructure services (identity, config, secrets).</summary>
        public static readonly SectorId Core = new("core");

        /// <summary>AI and machine learning services.</summary>
        public static readonly SectorId AI = new("ai");

        /// <summary>Operations and monitoring services.</summary>
        public static readonly SectorId Ops = new("ops");

        /// <summary>Data processing and analytics services.</summary>
        public static readonly SectorId Data = new("data");

        /// <summary>Web and API services.</summary>
        public static readonly SectorId Web = new("web");

        /// <summary>Messaging and event services.</summary>
        public static readonly SectorId Messaging = new("messaging");

        /// <summary>Storage and persistence services.</summary>
        public static readonly SectorId Storage = new("storage");
    }
}
