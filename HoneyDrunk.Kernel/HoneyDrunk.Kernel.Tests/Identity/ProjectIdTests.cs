using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class ProjectIdTests
{
    [Fact]
    public void Constructor_WithValidUlid_CreatesProjectId()
    {
        var ulid = Ulid.NewUlid();

        var projectId = new ProjectId(ulid);

        projectId.Value.Should().Be(ulid);
        projectId.ToString().Should().Be(ulid.ToString());
    }

    [Fact]
    public void Constructor_WithValidString_CreatesProjectId()
    {
        var ulidString = Ulid.NewUlid().ToString();

        var projectId = new ProjectId(ulidString);

        projectId.Value.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithNullOrWhitespace_ThrowsArgumentException(string? value)
    {
        var act = () => new ProjectId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-ulid")]
    [InlineData("12345")]
    public void Constructor_WithInvalidString_ThrowsArgumentException(string value)
    {
        var act = () => new ProjectId(value);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid ULID*");
    }

    [Fact]
    public void NewId_CreatesNewProjectId()
    {
        var projectId1 = ProjectId.NewId();
        var projectId2 = ProjectId.NewId();

        projectId1.Value.Should().NotBe(projectId2.Value);
        projectId1.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToUlid_ReturnsUlidValue()
    {
        var ulid = Ulid.NewUlid();
        var projectId = new ProjectId(ulid);

        var result = projectId.ToUlid();

        result.Should().Be(ulid);
    }

    [Fact]
    public void FromUlid_CreatesProjectId()
    {
        var ulid = Ulid.NewUlid();

        var projectId = ProjectId.FromUlid(ulid);

        projectId.Value.Should().Be(ulid);
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var ulidString = Ulid.NewUlid().ToString();

        var result = ProjectId.TryParse(ulidString, out var projectId);

        result.Should().BeTrue();
        projectId.Value.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-ulid")]
    [InlineData("")]
    public void TryParse_InvalidString_ReturnsFalse(string value)
    {
        var result = ProjectId.TryParse(value, out var projectId);

        result.Should().BeFalse();
        projectId.Should().Be(default(ProjectId));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var ulid = Ulid.NewUlid();
        var projectId = new ProjectId(ulid);

        string value = projectId;

        value.Should().Be(ulid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToUlid_ReturnsValue()
    {
        var ulid = Ulid.NewUlid();
        var projectId = new ProjectId(ulid);

        Ulid value = projectId;

        value.Should().Be(ulid);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var ulid = Ulid.NewUlid();
        var projectId1 = new ProjectId(ulid);
        var projectId2 = new ProjectId(ulid);

        projectId1.Should().Be(projectId2);
        (projectId1 == projectId2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var projectId1 = ProjectId.NewId();
        var projectId2 = ProjectId.NewId();

        projectId1.Should().NotBe(projectId2);
        (projectId1 != projectId2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsUlidString()
    {
        var ulid = Ulid.NewUlid();
        var projectId = new ProjectId(ulid);

        var result = projectId.ToString();

        result.Should().Be(ulid.ToString());
    }
}
