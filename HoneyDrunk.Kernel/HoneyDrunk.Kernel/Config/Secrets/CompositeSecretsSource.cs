using HoneyDrunk.Kernel.Abstractions.Config;

namespace HoneyDrunk.Kernel.Config.Secrets;

/// <summary>
/// Composite secrets source that checks multiple sources in order, returning the first match.
/// </summary>
/// <param name="sources">The collection of secret sources to query.</param>
public sealed class CompositeSecretsSource(IEnumerable<ISecretsSource> sources) : ISecretsSource
{
    private readonly ISecretsSource[] _sources = [.. sources];

    /// <inheritdoc />
    public bool TryGetSecret(string key, out string? value)
    {
        foreach (var source in _sources)
        {
            if (source.TryGetSecret(key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }
}
