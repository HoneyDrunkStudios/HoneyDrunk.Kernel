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
            try
            {
                if (source.TryGetSecret(key, out value))
                {
                    return true;
                }
            }
            catch
            {
                // Skip sources that throw exceptions and continue to the next source
            }
        }

        value = null;
        return false;
    }
}
