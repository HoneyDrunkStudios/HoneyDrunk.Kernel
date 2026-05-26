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
    private const string DocsBaseUri = "https://docs.honeydrunk.io/errors";
    private const string ValidationTypeUri = $"{DocsBaseUri}/validation";
    private const string NotFoundTypeUri = $"{DocsBaseUri}/not-found";
    private const string SecurityTypeUri = $"{DocsBaseUri}/security";
    private const string ConcurrencyTypeUri = $"{DocsBaseUri}/concurrency";
    private const string DependencyFailureTypeUri = $"{DocsBaseUri}/dependency-failure";
    private const string InternalTypeUri = $"{DocsBaseUri}/internal";
    private const string ValidationArgumentTypeUri = $"{DocsBaseUri}/validation-argument";
    private const string DependencyTimeoutTypeUri = $"{DocsBaseUri}/dependency-timeout";

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
                return Create(400, validation.Message, validation.ErrorCode?.Value, ValidationTypeUri);
            case NotFoundException notFound:
                return Create(404, notFound.Message, notFound.ErrorCode?.Value, NotFoundTypeUri);
            case SecurityException security:
                return Create(403, security.Message, security.ErrorCode?.Value, SecurityTypeUri);
            case ConcurrencyException concurrency:
                return Create(409, concurrency.Message, concurrency.ErrorCode?.Value, ConcurrencyTypeUri);
            case DependencyFailureException dependencyFailure:
                return Create(502, dependencyFailure.Message, dependencyFailure.ErrorCode?.Value, DependencyFailureTypeUri);
            case HoneyDrunkException honeyDrunk:
                return Create(500, honeyDrunk.Message, honeyDrunk.ErrorCode?.Value, InternalTypeUri);
        }

        // Map common BCL exceptions to validation semantics where helpful.
        if (exception is ArgumentException or FormatException)
        {
            var ec = new ErrorCode("validation.argument");
            return new ErrorClassification(400, exception.Message, ec.Value, ValidationArgumentTypeUri);
        }

        if (exception is TimeoutException)
        {
            var ec = new ErrorCode("dependency.timeout");
            return new ErrorClassification(504, exception.Message, ec.Value, DependencyTimeoutTypeUri);
        }

        // Unclassified -> null (caller may fall back to generic 500 handling).
        return null;
    }

    private static ErrorClassification Create(int status, string title, string? code, string? typeUri)
    {
        return new ErrorClassification(status, title, code, typeUri);
    }
}
