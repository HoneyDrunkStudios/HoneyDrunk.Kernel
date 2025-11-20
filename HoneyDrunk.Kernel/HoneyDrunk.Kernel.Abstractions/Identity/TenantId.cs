namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a strongly-typed Tenant identifier in the HoneyDrunk.OS Grid.
/// </summary>
/// <remarks>
/// TenantId represents a multi-tenant isolation boundary (e.g., customer, organization).
/// Format: ULID for uniqueness and sortability.
/// </remarks>
public readonly record struct TenantId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantId"/> struct from a Ulid.
    /// </summary>
    /// <param name="value">The Ulid value.</param>
    public TenantId(Ulid value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantId"/> struct from a string.
    /// </summary>
    /// <param name="value">The Ulid string value.</param>
    /// <exception cref="ArgumentException">Thrown if the string is not a valid Ulid.</exception>
    public TenantId(string value)
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
    /// Implicitly converts a TenantId to a string.
    /// </summary>
    /// <param name="tenantId">The TenantId to convert.</param>
    public static implicit operator string(TenantId tenantId) => tenantId.ToString();

    /// <summary>
    /// Implicitly converts a TenantId to a Ulid.
    /// </summary>
    /// <param name="tenantId">The TenantId to convert.</param>
    public static implicit operator Ulid(TenantId tenantId) => tenantId.Value;

    /// <summary>
    /// Creates a new TenantId with a new Ulid.
    /// </summary>
    /// <returns>A new TenantId.</returns>
    public static TenantId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Converts this TenantId to a Ulid.
    /// </summary>
    /// <returns>The Ulid value.</returns>
    public Ulid ToUlid() => Value;

    /// <summary>
    /// Creates a TenantId from a Ulid.
    /// </summary>
    /// <param name="ulid">The Ulid value.</param>
    /// <returns>A new TenantId.</returns>
    public static TenantId FromUlid(Ulid ulid) => new(ulid);

    /// <summary>
    /// Attempts to parse a string into a TenantId.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="tenantId">The parsed TenantId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string value, out TenantId tenantId)
    {
        if (Ulid.TryParse(value, out var ulid))
        {
            tenantId = new TenantId(ulid);
            return true;
        }

        tenantId = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
