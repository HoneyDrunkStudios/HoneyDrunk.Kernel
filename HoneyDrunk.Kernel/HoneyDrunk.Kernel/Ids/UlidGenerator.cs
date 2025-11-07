using HoneyDrunk.Kernel.Abstractions.Ids;

namespace HoneyDrunk.Kernel.Ids;

/// <summary>
/// ID generator using GUIDs (placeholder for future ULID implementation).
/// </summary>
public sealed class UlidGenerator : IIdGenerator
{
    /// <inheritdoc />
    public string NewString()
    {
        var guid = Guid.NewGuid();
        return Convert.ToHexString(guid.ToByteArray()).ToLowerInvariant();
    }

    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();
}
