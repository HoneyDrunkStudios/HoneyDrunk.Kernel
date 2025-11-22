namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Correlation identifier for tracking related operations.
/// </summary>
/// <remarks>
/// CorrelationId groups related operations across Nodes in the Grid.
/// Format: ULID for uniqueness and sortability.
/// </remarks>
public readonly record struct CorrelationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationId"/> struct from a Ulid.
    /// </summary>
    /// <param name="value">The Ulid value.</param>
    public CorrelationId(Ulid value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationId"/> struct from a string.
    /// </summary>
    /// <param name="value">The Ulid string value.</param>
    /// <exception cref="ArgumentException">Thrown if the string is not a valid Ulid.</exception>
    public CorrelationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (!Ulid.TryParse(value, out var ulid))
        {
            throw new ArgumentException("Value is not a valid ULID.", nameof(value));
        }

        Value = ulid;
    }

    /// <summary>
    /// Gets the Ulid value.
    /// </summary>
    public Ulid Value { get; }

    /// <summary>
    /// Implicitly converts a CorrelationId to a string.
    /// </summary>
    /// <param name="correlationId">The CorrelationId to convert.</param>
    public static implicit operator string(CorrelationId correlationId) => correlationId.ToString();

    /// <summary>
    /// Implicitly converts a CorrelationId to a Ulid.
    /// </summary>
    /// <param name="correlationId">The CorrelationId to convert.</param>
    public static implicit operator Ulid(CorrelationId correlationId) => correlationId.Value;

    /// <summary>
    /// Creates a new CorrelationId with a new Ulid.
    /// </summary>
    /// <returns>A new CorrelationId.</returns>
    public static CorrelationId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Converts this CorrelationId to a Ulid.
    /// </summary>
    /// <returns>The Ulid value.</returns>
    public Ulid ToUlid() => Value;

    /// <summary>
    /// Creates a CorrelationId from a Ulid.
    /// </summary>
    /// <param name="ulid">The Ulid value.</param>
    /// <returns>A new CorrelationId.</returns>
    public static CorrelationId FromUlid(Ulid ulid) => new(ulid);

    /// <summary>
    /// Attempts to parse a string into a CorrelationId.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="correlationId">The parsed CorrelationId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string value, out CorrelationId correlationId)
    {
        if (Ulid.TryParse(value, out var ulid))
        {
            correlationId = new CorrelationId(ulid);
            return true;
        }

        correlationId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
