namespace HoneyDrunk.Kernel.Abstractions.Config;

/// <summary>
/// Provides access to secrets from a secure store.
/// </summary>
public interface ISecretsSource
{
    /// <summary>
    /// Attempts to retrieve a secret value by key.
    /// </summary>
    /// <param name="key">The key identifying the secret.</param>
    /// <param name="value">The secret value if found; otherwise null.</param>
    /// <returns>True if the secret was found; otherwise false.</returns>
    bool TryGetSecret(string key, out string? value);
}
