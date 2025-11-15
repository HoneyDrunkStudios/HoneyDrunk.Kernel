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
    public void NewString_WhenCalled_ReturnsNonEmptyUlidWithCrockfordBase32Alphabet()
    {
        var gen = new UlidGenerator();
        var id = gen.NewString();

        id.Should().NotBeNullOrWhiteSpace();
        id.Length.Should().Be(26);
        id.All(IsCrockfordBase32).Should().BeTrue();
    }

    /// <summary>
    /// Verifies ULIDs are distinct when called multiple times.
    /// </summary>
    [Fact]
    public void NewString_WhenCalledMultipleTimes_ReturnsDistinctIds()
    {
        var gen = new UlidGenerator();
        var ids = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            var id = gen.NewString();
            ids.Add(id).Should().BeTrue($"ULID {id} should be unique");
        }

        ids.Should().HaveCount(100);
    }

    /// <summary>
    /// Verifies ULIDs are sortable and generally increase over time.
    /// </summary>
    [Fact]
    public void NewString_WhenCalledWithDelay_ReturnsLexicographicallySortedIds()
    {
        var gen = new UlidGenerator();
        var a = gen.NewString();
        
        Thread.Sleep(10);
        
        var b = gen.NewString();

        a.CompareTo(b).Should().BeLessThan(0, "ULIDs generated with sufficient time gap should be sortable");
    }

    /// <summary>
    /// Verifies GUID from ULID is not empty.
    /// </summary>
    [Fact]
    public void NewGuid_WhenCalled_ReturnsNonEmptyGuid()
    {
        var gen = new UlidGenerator();
        var guid = gen.NewGuid();

        guid.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Verifies GUIDs generated are distinct.
    /// </summary>
    [Fact]
    public void NewGuid_WhenCalledMultipleTimes_ReturnsDistinctGuids()
    {
        var gen = new UlidGenerator();
        var guids = new HashSet<Guid>();

        for (int i = 0; i < 100; i++)
        {
            var guid = gen.NewGuid();
            guids.Add(guid).Should().BeTrue($"GUID {guid} should be unique");
        }

        guids.Should().HaveCount(100);
    }

    private static bool IsCrockfordBase32(char c)
    {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        return alphabet.Contains(char.ToUpperInvariant(c));
    }
}
