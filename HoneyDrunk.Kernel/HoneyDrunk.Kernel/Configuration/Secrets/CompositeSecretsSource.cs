using HoneyDrunk.Kernel.Abstractions.Configuration;

namespace HoneyDrunk.Kernel.Configuration.Secrets;

/// <summary>
/// Composite secrets source that checks multiple sources in order, returning the first match.
/// Any source that throws an exception is skipped and the next source is tried.
/// </summary>
/// <param name="sources">The collection of secret sources to query.</param>
public sealed class CompositeSecretsSource(IEnumerable<ISecretsSource> sources) : ISecretsSource
{
    private readonly ISecretsSource[] sources = [.. sources];

    /// <inheritdoc />
    public bool TryGetSecret(string key, out string? value)
    {
        foreach (var source in this.sources)
        {
            if (TryGetSecret(source, key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetSecret(ISecretsSource source, string key, out string? value)
    {
        try
        {
            return source.TryGetSecret(key, out value);
        }
        catch (Exception)
        {
            // Preserve the composite contract by skipping any failing source.
            // A future logger can be added here if source-level diagnostics are needed.
        }

        value = null;
        return false;
    }
}
