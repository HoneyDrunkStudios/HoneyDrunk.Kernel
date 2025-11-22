using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class TenantIdTests
{
    [Fact]
    public void Constructor_FromUlid_CreatesTenantId()
    {
        // Arrange
        var ulid = Ulid.NewUlid();

        // Act
        var tenantId = new TenantId(ulid);

        // Assert
        tenantId.Value.Should().Be(ulid);
        tenantId.ToString().Should().Be(ulid.ToString());
    }

    [Fact]
    public void Constructor_FromValidString_CreatesTenantId()
    {
        // Arrange
        var ulidString = Ulid.NewUlid().ToString();

        // Act
        var tenantId = new TenantId(ulidString);

        // Assert
        tenantId.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespace_ThrowsArgumentException(string? value)
    {
        // Act
        var act = () => new TenantId(value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidUlidString_ThrowsArgumentException()
    {
        // Act
        var act = () => new TenantId("not-a-valid-ulid");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid ULID*");
    }

    [Fact]
    public void NewId_CreatesUniqueTenantId()
    {
        // Act
        var id1 = TenantId.NewId();
        var id2 = TenantId.NewId();

        // Assert
        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact]
    public void TryParse_ValidUlid_ReturnsTrue()
    {
        // Arrange
        var ulidString = Ulid.NewUlid().ToString();

        // Act
        var result = TenantId.TryParse(ulidString, out var tenantId);

        // Assert
        result.Should().BeTrue();
        tenantId.ToString().Should().Be(ulidString);
    }

    [Fact]
    public void TryParse_InvalidUlid_ReturnsFalse()
    {
        // Act
        var result = TenantId.TryParse("invalid", out var tenantId);

        // Assert
        result.Should().BeFalse();
        tenantId.Value.Should().Be(default);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsUlidString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var tenantId = new TenantId(ulid);

        // Act
        string value = tenantId;

        // Assert
        value.Should().Be(ulid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToUlid_ReturnsUlid()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var tenantId = new TenantId(ulid);

        // Act
        Ulid convertedUlid = tenantId;

        // Assert
        convertedUlid.Should().Be(ulid);
    }

    [Fact]
    public void ToUlid_ReturnsUnderlyingUlid()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var tenantId = new TenantId(ulid);

        // Act
        var result = tenantId.ToUlid();

        // Assert
        result.Should().Be(ulid);
    }

    [Fact]
    public void FromUlid_CreatesTenantId()
    {
        // Arrange
        var ulid = Ulid.NewUlid();

        // Act
        var tenantId = TenantId.FromUlid(ulid);

        // Assert
        tenantId.Value.Should().Be(ulid);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id1 = new TenantId(ulid);
        var id2 = new TenantId(ulid);

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        // Arrange
        var id1 = TenantId.NewId();
        var id2 = TenantId.NewId();

        // Act & Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }
}
