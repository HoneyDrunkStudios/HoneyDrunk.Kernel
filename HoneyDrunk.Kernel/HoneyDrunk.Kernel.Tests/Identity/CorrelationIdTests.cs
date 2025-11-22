using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class CorrelationIdTests
{
    [Fact]
    public void Constructor_WithValidUlid_CreatesCorrelationId()
    {
        var ulid = Ulid.NewUlid();

        var correlationId = new CorrelationId(ulid);

        correlationId.Value.Should().Be(ulid);
        correlationId.ToString().Should().Be(ulid.ToString());
    }

    [Fact]
    public void Constructor_WithValidString_CreatesCorrelationId()
    {
        var ulidString = Ulid.NewUlid().ToString();

        var correlationId = new CorrelationId(ulidString);

        correlationId.Value.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithNullOrWhitespace_ThrowsArgumentException(string? value)
    {
        var act = () => new CorrelationId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-ulid")]
    [InlineData("12345")]
    public void Constructor_WithInvalidString_ThrowsArgumentException(string value)
    {
        var act = () => new CorrelationId(value);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid ULID*");
    }

    [Fact]
    public void NewId_CreatesNewCorrelationId()
    {
        var correlationId1 = CorrelationId.NewId();
        var correlationId2 = CorrelationId.NewId();

        correlationId1.Value.Should().NotBe(correlationId2.Value);
        correlationId1.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToUlid_ReturnsUlidValue()
    {
        var ulid = Ulid.NewUlid();
        var correlationId = new CorrelationId(ulid);

        var result = correlationId.ToUlid();

        result.Should().Be(ulid);
    }

    [Fact]
    public void FromUlid_CreatesCorrelationId()
    {
        var ulid = Ulid.NewUlid();

        var correlationId = CorrelationId.FromUlid(ulid);

        correlationId.Value.Should().Be(ulid);
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var ulidString = Ulid.NewUlid().ToString();

        var result = CorrelationId.TryParse(ulidString, out var correlationId);

        result.Should().BeTrue();
        correlationId.Value.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-ulid")]
    [InlineData("")]
    public void TryParse_InvalidString_ReturnsFalse(string value)
    {
        var result = CorrelationId.TryParse(value, out var correlationId);

        result.Should().BeFalse();
        correlationId.Should().Be(default(CorrelationId));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var ulid = Ulid.NewUlid();
        var correlationId = new CorrelationId(ulid);

        string value = correlationId;

        value.Should().Be(ulid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToUlid_ReturnsValue()
    {
        var ulid = Ulid.NewUlid();
        var correlationId = new CorrelationId(ulid);

        Ulid value = correlationId;

        value.Should().Be(ulid);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var ulid = Ulid.NewUlid();
        var correlationId1 = new CorrelationId(ulid);
        var correlationId2 = new CorrelationId(ulid);

        correlationId1.Should().Be(correlationId2);
        (correlationId1 == correlationId2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var correlationId1 = CorrelationId.NewId();
        var correlationId2 = CorrelationId.NewId();

        correlationId1.Should().NotBe(correlationId2);
        (correlationId1 != correlationId2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsUlidString()
    {
        var ulid = Ulid.NewUlid();
        var correlationId = new CorrelationId(ulid);

        var result = correlationId.ToString();

        result.Should().Be(ulid.ToString());
    }
}
