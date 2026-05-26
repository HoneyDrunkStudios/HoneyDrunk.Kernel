using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Errors;

namespace HoneyDrunk.Kernel.Tests.Errors;

public class DefaultErrorClassifierTests
{
    private readonly DefaultErrorClassifier _classifier = new();

    [Fact]
    public void Classify_NullException_ReturnsNull()
    {
        _classifier.Classify(null!).Should().BeNull();
    }

    [Fact]
    public void Classify_ValidationException_Maps400AndValidationUri()
    {
        var ex = new ValidationException("bad input");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
        result.Title.Should().Be("bad input");
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/validation");
    }

    [Fact]
    public void Classify_NotFoundException_Maps404AndNotFoundUri()
    {
        var ex = new NotFoundException("missing");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/not-found");
    }

    [Fact]
    public void Classify_SecurityException_Maps403AndSecurityUri()
    {
        var ex = new SecurityException("forbidden");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(403);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/security");
    }

    [Fact]
    public void Classify_ConcurrencyException_Maps409AndConcurrencyUri()
    {
        var ex = new ConcurrencyException("conflict");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(409);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/concurrency");
    }

    [Fact]
    public void Classify_DependencyFailureException_Maps502AndDependencyFailureUri()
    {
        var ex = new DependencyFailureException("upstream down");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(502);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/dependency-failure");
    }

    [Fact]
    public void Classify_HoneyDrunkException_Maps500AndInternalUri()
    {
        var ex = new HoneyDrunkException("internal");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/internal");
    }

    [Fact]
    public void Classify_ArgumentException_Maps400AndValidationArgumentUri()
    {
        var ex = new ArgumentException("bad arg");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/validation-argument");
        result.ErrorCode.Should().Be("validation.argument");
    }

    [Fact]
    public void Classify_FormatException_Maps400AndValidationArgumentUri()
    {
        var ex = new FormatException("bad format");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/validation-argument");
    }

    [Fact]
    public void Classify_TimeoutException_Maps504AndDependencyTimeoutUri()
    {
        var ex = new TimeoutException("timeout");

        var result = _classifier.Classify(ex);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(504);
        result.TypeUri.Should().Be("https://docs.honeydrunk.io/errors/dependency-timeout");
        result.ErrorCode.Should().Be("dependency.timeout");
    }

    [Fact]
    public void Classify_UnknownException_ReturnsNull()
    {
        var ex = new InvalidOperationException("unclassified");

        _classifier.Classify(ex).Should().BeNull();
    }
}
