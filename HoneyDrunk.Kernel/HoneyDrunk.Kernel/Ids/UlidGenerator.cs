using HoneyDrunk.Kernel.Abstractions.Ids;

namespace HoneyDrunk.Kernel.Ids;

/// <summary>
/// Generates ULID-based identifiers (Universally Unique Lexicographically Sortable Identifier).
/// </summary>
public sealed class UlidGenerator : IIdGenerator
{
    /// <inheritdoc />
    public string NewString() => Ulid.NewUlid().ToString();

    /// <inheritdoc />
    public Guid NewGuid() => Ulid.NewUlid().ToGuid();
}
