using System.Text.RegularExpressions;

namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Node identifier in the HoneyDrunk.OS Grid.
/// </summary>
/// <remarks>
/// NodeId is a validated identifier following kebab-case conventions.
/// Format: lowercase letters, digits, and hyphens only. Length: 3-64 characters.
/// No consecutive hyphens, and cannot start or end with a hyphen.
/// Examples: "payment-node", "auth-gateway", "notification-service".
/// </remarks>
public readonly partial record struct NodeId
{
    private static readonly Regex ValidationPattern = ValidationRegex();
    private const int MinLength = 3;
    private const int MaxLength = 64;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeId"/> struct.
    /// </summary>
    /// <param name="value">The Node identifier value.</param>
    /// <exception cref="ArgumentException">Thrown if the value is invalid.</exception>
    public NodeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (!IsValid(value, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the Node identifier value.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    /// Implicitly converts a NodeId to a string.
    /// </summary>
    /// <param name="nodeId">The NodeId to convert.</param>
    public static implicit operator string(NodeId nodeId) => nodeId.Value;

    /// <summary>
    /// Validates whether a string is a valid Node identifier.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(string value, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Node ID cannot be null or whitespace.";
            return false;
        }

        if (value.Length < MinLength || value.Length > MaxLength)
        {
            errorMessage = $"Node ID must be between {MinLength} and {MaxLength} characters.";
            return false;
        }

        if (!ValidationPattern.IsMatch(value))
        {
            errorMessage = "Node ID must be kebab-case: lowercase letters, digits, and hyphens only. Cannot have consecutive hyphens or start/end with hyphens.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Attempts to parse a string into a NodeId.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="nodeId">The parsed NodeId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string value, out NodeId nodeId)
    {
        if (IsValid(value, out _))
        {
            nodeId = new NodeId(value);
            return true;
        }

        nodeId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();
}
