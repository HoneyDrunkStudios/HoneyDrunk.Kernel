namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents the identifier of the operation that directly triggered (caused) the current operation.
/// </summary>
/// <remarks>
/// A <see cref="CausationId"/> is a ULID forming a causal chain across distributed operations.
/// Each downstream operation gets a new <see cref="CorrelationId"/> but preserves the parent's
/// correlation as its causation. This enables reconstruction of execution trees.
/// </remarks>
public readonly record struct CausationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CausationId"/> struct from a ULID value.
    /// </summary>
    /// <param name="value">The ULID value.</param>
    public CausationId(Ulid value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CausationId"/> struct from a string.
    /// </summary>
    /// <param name="value">The ULID string.</param>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid ULID.</exception>
    public CausationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (!Ulid.TryParse(value, out var ulid))
        {
            throw new ArgumentException("Value is not a valid ULID.", nameof(value));
        }

        Value = ulid;
    }

    /// <summary>
    /// Gets the underlying ULID value.
    /// </summary>
    public Ulid Value { get; }

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    /// <param name="id">Source causation identifier.</param>
    /// <returns>The ULID string representation.</returns>
    public static implicit operator string(CausationId id) => id.Value.ToString();

    /// <summary>
    /// Implicit conversion to ULID.
    /// </summary>
    /// <param name="id">Source causation identifier.</param>
    /// <returns>The underlying ULID value.</returns>
    public static implicit operator Ulid(CausationId id) => id.Value;

    /// <summary>
    /// Attempts to parse a string into a <see cref="CausationId"/>.
    /// </summary>
    /// <param name="value">Candidate string.</param>
    /// <param name="causationId">Parsed identifier when successful.</param>
    /// <returns>True if parsing succeeds; otherwise false.</returns>
    public static bool TryParse(string? value, out CausationId causationId)
    {
        if (!string.IsNullOrWhiteSpace(value) && Ulid.TryParse(value, out var ulid))
        {
            causationId = new CausationId(ulid);
            return true;
        }

        causationId = default;
        return false;
    }

    /// <summary>
    /// Creates a causation identifier from a ULID.
    /// </summary>
    /// <param name="ulid">Source ULID.</param>
    /// <returns>A new <see cref="CausationId"/> instance.</returns>
    public static CausationId FromUlid(Ulid ulid) => new(ulid);

    /// <summary>
    /// Converts to the underlying ULID.
    /// </summary>
    /// <returns>The underlying ULID value.</returns>
    public Ulid ToUlid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
