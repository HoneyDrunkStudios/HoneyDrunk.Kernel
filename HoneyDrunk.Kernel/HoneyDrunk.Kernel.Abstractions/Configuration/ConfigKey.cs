namespace HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Represents a strongly-typed configuration key.
/// </summary>
/// <remarks>
/// ConfigKey enforces consistent key naming and validation.
/// Supports hierarchical keys with colon separation (e.g., "Database:ConnectionString").
/// </remarks>
public readonly record struct ConfigKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigKey"/> struct.
    /// </summary>
    /// <param name="value">The configuration key value.</param>
    public ConfigKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Value = value;
    }

    /// <summary>
    /// Gets the key value.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    /// Gets the parent key (if this is a hierarchical key).
    /// </summary>
    public ConfigKey? Parent
    {
        get
        {
            var segments = GetSegments();
            if (segments.Length <= 1)
            {
                return null;
            }

            return new ConfigKey(string.Join(":", segments[..^1]));
        }
    }

    /// <summary>
    /// Implicitly converts a ConfigKey to a string.
    /// </summary>
    /// <param name="key">The ConfigKey to convert.</param>
    public static implicit operator string(ConfigKey key) => key.Value;

    /// <summary>
    /// Implicitly converts a string to a ConfigKey.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator ConfigKey(string value) => new(value);

    /// <summary>
    /// Gets the segments of this key (split by colon).
    /// </summary>
    /// <returns>An array of key segments.</returns>
    public string[] GetSegments() => Value.Split(':', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Creates a child key.
    /// </summary>
    /// <param name="segment">The child segment.</param>
    /// <returns>A new child key.</returns>
    public ConfigKey CreateChild(string segment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment, nameof(segment));
        return new ConfigKey($"{Value}:{segment}");
    }

    /// <summary>
    /// Creates a ConfigKey from a string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new ConfigKey.</returns>
    public static ConfigKey FromString(string value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value;
}
