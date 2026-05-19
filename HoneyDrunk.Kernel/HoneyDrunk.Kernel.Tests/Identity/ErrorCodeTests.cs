using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class ErrorCodeTests
{
    [Theory]
    [InlineData("validation.input")]
    [InlineData("authentication.token-expired")]
    [InlineData("rate-limit.exceeded")]
    [InlineData("a")]
    [InlineData("segment-1.segment-2.segment-3")]
    public void Constructor_WithValidValue_CreatesErrorCode(string value)
    {
        var code = new ErrorCode(value);

        code.Value.Should().Be(value);
        code.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Validation.Input")]
    [InlineData("validation_input")]
    [InlineData("validation..input")]
    [InlineData("validation.input!")]
    public void Constructor_WithInvalidValue_ThrowsArgumentException(string? value)
    {
        var act = () => new ErrorCode(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithTooLongValue_ThrowsArgumentException()
    {
        var value = new string('a', 129);

        var act = () => new ErrorCode(value);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*128 characters*");
    }

    [Fact]
    public void Constructor_WithTooLongSegment_ThrowsArgumentException()
    {
        var value = $"{new string('a', 33)}.input";

        var act = () => new ErrorCode(value);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*between 1 and 32*");
    }

    [Theory]
    [InlineData("validation.input", true)]
    [InlineData("resource.notfound", true)]
    [InlineData("Validation.Input", false)]
    [InlineData("validation..input", false)]
    [InlineData("", false)]
    public void IsValid_ReturnsExpectedResult(string value, bool expected)
    {
        var result = ErrorCode.IsValid(value, out var errorMessage);

        result.Should().Be(expected);
        if (expected)
        {
            errorMessage.Should().BeNull();
        }
        else
        {
            errorMessage.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void TryParse_WithValidValue_ReturnsTrueAndCode()
    {
        var result = ErrorCode.TryParse("configuration.invalid", out var code);

        result.Should().BeTrue();
        code.Value.Should().Be("configuration.invalid");
    }

    [Fact]
    public void TryParse_WithInvalidValue_ReturnsFalseAndDefault()
    {
        var result = ErrorCode.TryParse("configuration_invalid", out var code);

        result.Should().BeFalse();
        code.Should().Be(default(ErrorCode));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var code = new ErrorCode("dependency.timeout");

        string value = code;

        value.Should().Be("dependency.timeout");
    }

    [Fact]
    public void WellKnown_CodesUseExpectedValues()
    {
        ErrorCode.WellKnown.ValidationInput.Value.Should().Be("validation.input");
        ErrorCode.WellKnown.AuthenticationTokenExpired.Value.Should().Be("authentication.token-expired");
        ErrorCode.WellKnown.ContextMissing.Value.Should().Be("context.missing");
        ErrorCode.WellKnown.ResourceConflict.Value.Should().Be("resource.conflict");
        ErrorCode.WellKnown.StateVersionConflict.Value.Should().Be("state.version-conflict");
        ErrorCode.WellKnown.ContractMissingField.Value.Should().Be("contract.missing-field");
        ErrorCode.WellKnown.FeatureNotAllowed.Value.Should().Be("feature.not-allowed");
        ErrorCode.WellKnown.DependencyUnavailable.Value.Should().Be("dependency.unavailable");
        ErrorCode.WellKnown.InternalError.Value.Should().Be("internal.error");
        ErrorCode.WellKnown.RateLimitExceeded.Value.Should().Be("rate-limit.exceeded");
    }
}
