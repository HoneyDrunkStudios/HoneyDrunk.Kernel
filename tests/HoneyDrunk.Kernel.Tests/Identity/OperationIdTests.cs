using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class OperationIdTests
{
    [Fact]
    public void Constructor_WithUlid_CreatesOperationId()
    {
        var ulid = Ulid.NewUlid();

        var id = new OperationId(ulid);

        id.Value.Should().Be(ulid);
        id.ToString().Should().Be(ulid.ToString());
    }

    [Fact]
    public void Constructor_WithString_CreatesOperationId()
    {
        var value = Ulid.NewUlid().ToString();

        var id = new OperationId(value);

        id.Value.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ulid")]
    public void Constructor_WithInvalidString_ThrowsArgumentException(string? value)
    {
        var act = () => new OperationId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewId_CreatesUniqueIds()
    {
        var first = OperationId.NewId();
        var second = OperationId.NewId();

        first.Should().NotBe(second);
        first.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromUlid_CreatesOperationId()
    {
        var ulid = Ulid.NewUlid();

        var id = OperationId.FromUlid(ulid);

        id.Value.Should().Be(ulid);
    }

    [Fact]
    public void TryParse_WithValidValue_ReturnsTrue()
    {
        var value = Ulid.NewUlid().ToString();

        var result = OperationId.TryParse(value, out var id);

        result.Should().BeTrue();
        id.ToString().Should().Be(value);
    }

    [Fact]
    public void TryParse_WithInvalidValue_ReturnsFalse()
    {
        var result = OperationId.TryParse("invalid", out var id);

        result.Should().BeFalse();
        id.Should().Be(default(OperationId));
    }

    [Fact]
    public void ToUlid_ReturnsUnderlyingValue()
    {
        var ulid = Ulid.NewUlid();
        var id = new OperationId(ulid);

        id.ToUlid().Should().Be(ulid);
    }

    [Fact]
    public void ImplicitConversions_ReturnUnderlyingValues()
    {
        var ulid = Ulid.NewUlid();
        var id = new OperationId(ulid);

        string stringValue = id;
        Ulid ulidValue = id;

        stringValue.Should().Be(ulid.ToString());
        ulidValue.Should().Be(ulid);
    }
}
