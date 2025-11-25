using System.Text.RegularExpressions;

namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Environment identifier (e.g., production, staging, development) within the Grid.
/// </summary>
/// <remarks>
/// Environment identifiers are low-cardinality values used for partitioning telemetry, configuration,
/// and operational behavior. They must be kebab-case (lowercase letters, digits, single hyphens) and
/// between 3 and 32 characters. Examples: "production", "staging", "dev-alice", "perf-test".
/// </remarks>
public readonly partial record struct EnvironmentId
{
    private static readonly Regex ValidationPattern = ValidationRegex();
    private const int MinLength = 3;
    private const int MaxLength = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentId"/> struct.
    /// </summary>
    /// <param name="value">The environment identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
    public EnvironmentId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (!IsValid(value, out var error))
        {
            throw new ArgumentException(error, nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying string value.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    /// Implicit conversion to <see cref="string"/>.
    /// </summary>
    /// <param name="id">The environment identifier to convert.</param>
    public static implicit operator string(EnvironmentId id) => id.Value;

    /// <summary>
    /// Validates a candidate value for <see cref="EnvironmentId"/>.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    /// <param name="errorMessage">Error message when invalid.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(string? value, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Environment ID cannot be null or whitespace.";
            return false;
        }

        if (value.Length < MinLength || value.Length > MaxLength)
        {
            errorMessage = $"Environment ID must be between {MinLength} and {MaxLength} characters.";
            return false;
        }

        if (!ValidationPattern.IsMatch(value))
        {
            errorMessage = "Environment ID must be kebab-case: lowercase letters, digits, and single hyphens only.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Attempts to parse a string into an <see cref="EnvironmentId"/>.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    /// <param name="environmentId">Parsed identifier when successful.</param>
    /// <returns>True if parsing succeeds; otherwise false.</returns>
    public static bool TryParse(string? value, out EnvironmentId environmentId)
    {
        if (IsValid(value, out _))
        {
            environmentId = new EnvironmentId(value!);
            return true;
        }

        environmentId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    /// <summary>
    /// Well-known environment identifiers for common deployment environments.
    /// </summary>
    public static class WellKnown
    {
        /// <summary>Production environment.</summary>
        public static readonly EnvironmentId Production = new("production");

        /// <summary>Staging environment.</summary>
        public static readonly EnvironmentId Staging = new("staging");

        /// <summary>Development environment.</summary>
        public static readonly EnvironmentId Development = new("development");

        /// <summary>Testing environment.</summary>
        public static readonly EnvironmentId Testing = new("testing");

        /// <summary>Performance testing environment.</summary>
        public static readonly EnvironmentId Performance = new("performance");

        /// <summary>Integration testing environment.</summary>
        public static readonly EnvironmentId Integration = new("integration");

        /// <summary>Local development environment.</summary>
        public static readonly EnvironmentId Local = new("local");
    }
}
