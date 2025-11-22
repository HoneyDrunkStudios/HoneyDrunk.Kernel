namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Run identifier for tracking execution instances.
/// </summary>
/// <remarks>
/// RunId represents a specific execution/run of an operation, workflow, or job.
/// Format: ULID for uniqueness and sortability.
/// </remarks>
public readonly record struct RunId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunId"/> struct from a Ulid.
    /// </summary>
    /// <param name="value">The Ulid value.</param>
    public RunId(Ulid value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunId"/> struct from a string.
    /// </summary>
    /// <param name="value">The Ulid string value.</param>
    /// <exception cref="ArgumentException">Thrown if the string is not a valid Ulid.</exception>
    public RunId(string value)
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
    /// Implicitly converts a RunId to a string.
    /// </summary>
    /// <param name="runId">The RunId to convert.</param>
    public static implicit operator string(RunId runId) => runId.ToString();

    /// <summary>
    /// Implicitly converts a RunId to a Ulid.
    /// </summary>
    /// <param name="runId">The RunId to convert.</param>
    public static implicit operator Ulid(RunId runId) => runId.Value;

    /// <summary>
    /// Creates a new RunId with a new Ulid.
    /// </summary>
    /// <returns>A new RunId.</returns>
    public static RunId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Converts this RunId to a Ulid.
    /// </summary>
    /// <returns>The Ulid value.</returns>
    public Ulid ToUlid() => Value;

    /// <summary>
    /// Creates a RunId from a Ulid.
    /// </summary>
    /// <param name="ulid">The Ulid value.</param>
    /// <returns>A new RunId.</returns>
    public static RunId FromUlid(Ulid ulid) => new(ulid);

    /// <summary>
    /// Attempts to parse a string into a RunId.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="runId">The parsed RunId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string value, out RunId runId)
    {
        if (Ulid.TryParse(value, out var ulid))
        {
            runId = new RunId(ulid);
            return true;
        }

        runId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
