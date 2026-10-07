namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a unique business identifier for a single operation.
/// </summary>
/// <remarks>
/// OperationId uniquely identifies a unit of work (HTTP request, message handler, job step, etc.)
/// within a business operation chain. Together with CorrelationId and CausationId,
/// it describes business relationships independently of Activity trace and span IDs.
/// </remarks>
public readonly record struct OperationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationId"/> struct from a ULID.
    /// </summary>
    /// <param name="value">The ULID value.</param>
    public OperationId(Ulid value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationId"/> struct from a string.
    /// </summary>
    /// <param name="value">The ULID string representation.</param>
    /// <exception cref="ArgumentException">Thrown if the value is not a valid ULID.</exception>
    public OperationId(string value)
    {
        Value = UlidIdentity.Parse(value);
    }

    /// <summary>
    /// Gets the underlying ULID value.
    /// </summary>
    public Ulid Value { get; }

    /// <summary>
    /// Implicitly converts an OperationId to a string.
    /// </summary>
    /// <param name="operationId">The OperationId to convert.</param>
    public static implicit operator string(OperationId operationId) => operationId.ToString();

    /// <summary>
    /// Implicitly converts an OperationId to a Ulid.
    /// </summary>
    /// <param name="operationId">The OperationId to convert.</param>
    public static implicit operator Ulid(OperationId operationId) => operationId.Value;

    /// <summary>
    /// Generates a new operation identifier.
    /// </summary>
    /// <returns>A new OperationId with a unique ULID.</returns>
    public static OperationId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Creates an OperationId from a ULID.
    /// </summary>
    /// <param name="ulid">The ULID value.</param>
    /// <returns>An OperationId instance.</returns>
    public static OperationId FromUlid(Ulid ulid) => new(ulid);

    /// <summary>
    /// Attempts to parse a string into an OperationId.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="operationId">The parsed OperationId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? value, out OperationId operationId)
    {
        if (UlidIdentity.TryParse(value, out var ulid))
        {
            operationId = new OperationId(ulid);
            return true;
        }

        operationId = default;
        return false;
    }

    /// <summary>
    /// Converts the OperationId to its ULID representation.
    /// </summary>
    /// <returns>The underlying ULID value.</returns>
    public Ulid ToUlid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
