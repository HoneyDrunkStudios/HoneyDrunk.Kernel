using FluentAssertions;
using HoneyDrunk.Kernel.Ids;

namespace HoneyDrunk.Kernel.Tests.Ids;

/// <summary>
/// Tests for <see cref="UlidGenerator"/>.
/// </summary>
public class UlidGeneratorTests
{
    /// <summary>
    /// Verifies ULID string is non-empty and uses Crockford Base32 alphabet.
    /// </summary>
    [Fact]
    public void NewString_ShouldReturn_NonEmpty_UlidLike_Id()
    {
        var gen = new UlidGenerator();
        var id = gen.NewString();

        id.Should().NotBeNullOrWhiteSpace();
        id.Length.Should().Be(26);
        id.All(IsCrockfordBase32).Should().BeTrue();
    }

    /// <summary>
    /// Verifies ULIDs are distinct and roughly ordered by creation time.
    /// </summary>
    [Fact]
    public void NewString_ShouldProduce_DistinctAndRoughlyOrdered_Ids()
    {
        var gen = new UlidGenerator();
        var a = gen.NewString();
        Thread.Sleep(2);
        var b = gen.NewString();

        a.Should().NotBe(b);
        a.CompareTo(b).Should().BeLessThan(0);
    }

    /// <summary>
    /// Verifies GUID from ULID is not empty.
    /// </summary>
    [Fact]
    public void NewGuid_ShouldReturn_NonEmpty_Guid()
    {
        var gen = new UlidGenerator();
        var guid = gen.NewGuid();

        guid.Should().NotBe(Guid.Empty);
    }

    private static bool IsCrockfordBase32(char c)
    {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        return alphabet.Contains(char.ToUpperInvariant(c));
    }
}
