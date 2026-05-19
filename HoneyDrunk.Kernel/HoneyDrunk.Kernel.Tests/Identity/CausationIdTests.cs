using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class CausationIdTests
{
    [Fact]
    public void Constructor_WithUlid_CreatesCausationId()
    {
        var ulid = Ulid.NewUlid();

        var id = new CausationId(ulid);

        id.Value.Should().Be(ulid);
        id.ToString().Should().Be(ulid.ToString());
    }

    [Fact]
    public void Constructor_WithString_CreatesCausationId()
    {
        var value = Ulid.NewUlid().ToString();

        var id = new CausationId(value);

        id.Value.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ulid")]
    public void Constructor_WithInvalidString_ThrowsArgumentException(string? value)
    {
        var act = () => new CausationId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParse_WithValidValue_ReturnsTrue()
    {
        var value = Ulid.NewUlid().ToString();

        var result = CausationId.TryParse(value, out var id);

        result.Should().BeTrue();
        id.ToString().Should().Be(value);
    }

    [Fact]
    public void TryParse_WithInvalidValue_ReturnsFalse()
    {
        var result = CausationId.TryParse("invalid", out var id);

        result.Should().BeFalse();
        id.Should().Be(default(CausationId));
    }

    [Fact]
    public void FromUlid_CreatesCausationId()
    {
        var ulid = Ulid.NewUlid();

        var id = CausationId.FromUlid(ulid);

        id.Value.Should().Be(ulid);
    }

    [Fact]
    public void ToUlid_ReturnsUnderlyingValue()
    {
        var ulid = Ulid.NewUlid();
        var id = new CausationId(ulid);

        id.ToUlid().Should().Be(ulid);
    }

    [Fact]
    public void ImplicitConversions_ReturnUnderlyingValues()
    {
        var ulid = Ulid.NewUlid();
        var id = new CausationId(ulid);

        string stringValue = id;
        Ulid ulidValue = id;

        stringValue.Should().Be(ulid.ToString());
        ulidValue.Should().Be(ulid);
    }
}
