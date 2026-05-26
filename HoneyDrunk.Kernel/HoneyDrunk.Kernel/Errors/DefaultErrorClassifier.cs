using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Abstractions.Identity;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Errors;

/// <summary>
/// Default implementation of <see cref="IErrorClassifier"/> mapping Kernel exceptions to transport-friendly classifications.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via DI container registration.")]
internal sealed class DefaultErrorClassifier : IErrorClassifier
{
    /// <summary>
    /// Classifies an exception into a transport-friendly shape.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>Classification result or null if unrecognized.</returns>
    public ErrorClassification? Classify(Exception exception)
    {
        if (exception is null)
        {
            return null;
        }

        // Explicit Kernel exception hierarchy first.
        switch (exception)
        {
            case ValidationException validation:
                return Create(400, validation.Message, validation.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/validation");
            case NotFoundException notFound:
                return Create(404, notFound.Message, notFound.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/not-found");
            case SecurityException security:
                return Create(403, security.Message, security.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/security");
            case ConcurrencyException concurrency:
                return Create(409, concurrency.Message, concurrency.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/concurrency");
            case DependencyFailureException dependencyFailure:
                return Create(502, dependencyFailure.Message, dependencyFailure.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/dependency-failure");
            case HoneyDrunkException honeyDrunk:
                return Create(500, honeyDrunk.Message, honeyDrunk.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/internal");
        }

        // Map common BCL exceptions to validation semantics where helpful.
        if (exception is ArgumentException or FormatException)
        {
            var ec = new ErrorCode("validation.argument");
            return new ErrorClassification(400, exception.Message, ec.Value, "https://docs.honeydrunk.io/errors/validation-argument");
        }

        if (exception is TimeoutException)
        {
            var ec = new ErrorCode("dependency.timeout");
            return new ErrorClassification(504, exception.Message, ec.Value, "https://docs.honeydrunk.io/errors/dependency-timeout");
        }

        // Unclassified -> null (caller may fall back to generic 500 handling).
        return null;
    }

    private static ErrorClassification Create(int status, string title, string? code, string? typeUri)
    {
        return new ErrorClassification(status, title, code, typeUri);
    }
}
