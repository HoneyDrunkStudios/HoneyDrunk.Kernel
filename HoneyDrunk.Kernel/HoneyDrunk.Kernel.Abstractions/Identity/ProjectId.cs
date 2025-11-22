namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Project identifier in the HoneyDrunk.OS Grid.
/// </summary>
/// <remarks>
/// ProjectId represents a project or workspace within a tenant.
/// Format: ULID for uniqueness and sortability.
/// </remarks>
public readonly record struct ProjectId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectId"/> struct from a Ulid.
    /// </summary>
    /// <param name="value">The Ulid value.</param>
    public ProjectId(Ulid value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectId"/> struct from a string.
    /// </summary>
    /// <param name="value">The Ulid string value.</param>
    /// <exception cref="ArgumentException">Thrown if the string is not a valid Ulid.</exception>
    public ProjectId(string value)
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
    /// Implicitly converts a ProjectId to a string.
    /// </summary>
    /// <param name="projectId">The ProjectId to convert.</param>
    public static implicit operator string(ProjectId projectId) => projectId.ToString();

    /// <summary>
    /// Implicitly converts a ProjectId to a Ulid.
    /// </summary>
    /// <param name="projectId">The ProjectId to convert.</param>
    public static implicit operator Ulid(ProjectId projectId) => projectId.Value;

    /// <summary>
    /// Creates a new ProjectId with a new Ulid.
    /// </summary>
    /// <returns>A new ProjectId.</returns>
    public static ProjectId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Converts this ProjectId to a Ulid.
    /// </summary>
    /// <returns>The Ulid value.</returns>
    public Ulid ToUlid() => Value;

    /// <summary>
    /// Creates a ProjectId from a Ulid.
    /// </summary>
    /// <param name="ulid">The Ulid value.</param>
    /// <returns>A new ProjectId.</returns>
    public static ProjectId FromUlid(Ulid ulid) => new(ulid);

    /// <summary>
    /// Attempts to parse a string into a ProjectId.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="projectId">The parsed ProjectId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string value, out ProjectId projectId)
    {
        if (Ulid.TryParse(value, out var ulid))
        {
            projectId = new ProjectId(ulid);
            return true;
        }

        projectId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
