namespace HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Represents a fully-qualified configuration path including scope and key.
/// </summary>
/// <remarks>
/// ConfigPath combines scope and key to form a complete configuration address.
/// Example: "studio:honeycomb/Database:ConnectionString".
/// Format: "{scope-type}:{scope-id}/{key}"}.
/// </remarks>
public readonly record struct ConfigPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigPath"/> struct.
    /// </summary>
    /// <param name="scope">The configuration scope.</param>
    /// <param name="key">The configuration key.</param>
    public ConfigPath(IConfigScope scope, ConfigKey key)
    {
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));
        Scope = scope;
        Key = key;
    }

    /// <summary>
    /// Gets the configuration scope.
    /// </summary>
    public IConfigScope Scope { get; }

    /// <summary>
    /// Gets the configuration key.
    /// </summary>
    public ConfigKey Key { get; }

    /// <summary>
    /// Gets the full path string.
    /// </summary>
    public string FullPath => $"{Scope.ScopePath}/{Key.Value}";

    /// <summary>
    /// Parses a configuration path string.
    /// </summary>
    /// <param name="pathString">The path string to parse.</param>
    /// <param name="scopeFactory">Factory to create scope instances.</param>
    /// <returns>The parsed ConfigPath.</returns>
    public static ConfigPath Parse(string pathString, Func<string, IConfigScope> scopeFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathString, nameof(pathString));
        ArgumentNullException.ThrowIfNull(scopeFactory, nameof(scopeFactory));

        var parts = pathString.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid config path format. Expected: {scope}/{key}.", nameof(pathString));
        }

        var scope = scopeFactory(parts[0]);
        var key = new ConfigKey(parts[1]);

        return new ConfigPath(scope, key);
    }

    /// <inheritdoc />
    public override string ToString() => FullPath;
}
