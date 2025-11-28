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
        if (exception is ValidationException ve)
        {
            return Create(400, ve.Message, ve.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/validation");
        }
        else if (exception is NotFoundException nf)
        {
            return Create(404, nf.Message, nf.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/not-found");
        }
        else if (exception is SecurityException se)
        {
            return Create(403, se.Message, se.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/security");
        }
        else if (exception is ConcurrencyException cc)
        {
            return Create(409, cc.Message, cc.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/concurrency");
        }
        else if (exception is DependencyFailureException df)
        {
            return Create(502, df.Message, df.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/dependency-failure");
        }
        else if (exception is HoneyDrunkException hd)
        {
            // Generic HoneyDrunkException fallback (unclassified) -> 500.
            return Create(500, hd.Message, hd.ErrorCode?.Value, "https://docs.honeydrunk.io/errors/internal");
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
