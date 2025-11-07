namespace HoneyDrunk.Kernel.Abstractions.Ids;

/// <summary>
/// Generates unique identifiers for correlation and tracing.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier as a string.
    /// </summary>
    /// <returns>A unique string identifier.</returns>
    string NewString();

    /// <summary>
    /// Generates a new globally unique identifier.
    /// </summary>
    /// <returns>A new GUID.</returns>
    Guid NewGuid();
}
