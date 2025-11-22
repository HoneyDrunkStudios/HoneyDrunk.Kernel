using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class RunIdTests
{
    [Fact]
    public void Constructor_WithValidUlid_CreatesRunId()
    {
        var ulid = Ulid.NewUlid();

        var runId = new RunId(ulid);

        runId.Value.Should().Be(ulid);
        runId.ToString().Should().Be(ulid.ToString());
    }

    [Fact]
    public void Constructor_WithValidString_CreatesRunId()
    {
        var ulidString = Ulid.NewUlid().ToString();

        var runId = new RunId(ulidString);

        runId.Value.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithNullOrWhitespace_ThrowsArgumentException(string? value)
    {
        var act = () => new RunId(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-ulid")]
    [InlineData("12345")]
    public void Constructor_WithInvalidString_ThrowsArgumentException(string value)
    {
        var act = () => new RunId(value);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid ULID*");
    }

    [Fact]
    public void NewId_CreatesNewRunId()
    {
        var runId1 = RunId.NewId();
        var runId2 = RunId.NewId();

        runId1.Value.Should().NotBe(runId2.Value);
        runId1.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToUlid_ReturnsUlidValue()
    {
        var ulid = Ulid.NewUlid();
        var runId = new RunId(ulid);

        var result = runId.ToUlid();

        result.Should().Be(ulid);
    }

    [Fact]
    public void FromUlid_CreatesRunId()
    {
        var ulid = Ulid.NewUlid();

        var runId = RunId.FromUlid(ulid);

        runId.Value.Should().Be(ulid);
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var ulidString = Ulid.NewUlid().ToString();

        var result = RunId.TryParse(ulidString, out var runId);

        result.Should().BeTrue();
        runId.Value.ToString().Should().Be(ulidString);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-ulid")]
    [InlineData("")]
    public void TryParse_InvalidString_ReturnsFalse(string value)
    {
        var result = RunId.TryParse(value, out var runId);

        result.Should().BeFalse();
        runId.Should().Be(default(RunId));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var ulid = Ulid.NewUlid();
        var runId = new RunId(ulid);

        string value = runId;

        value.Should().Be(ulid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToUlid_ReturnsValue()
    {
        var ulid = Ulid.NewUlid();
        var runId = new RunId(ulid);

        Ulid value = runId;

        value.Should().Be(ulid);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var ulid = Ulid.NewUlid();
        var runId1 = new RunId(ulid);
        var runId2 = new RunId(ulid);

        runId1.Should().Be(runId2);
        (runId1 == runId2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var runId1 = RunId.NewId();
        var runId2 = RunId.NewId();

        runId1.Should().NotBe(runId2);
        (runId1 != runId2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsUlidString()
    {
        var ulid = Ulid.NewUlid();
        var runId = new RunId(ulid);

        var result = runId.ToString();

        result.Should().Be(ulid.ToString());
    }
}
