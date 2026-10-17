namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Contract for mapping exceptions to <see cref="ErrorClassification"/> results.
/// </summary>
public interface IErrorClassifier
{
    /// <summary>
    /// Classifies an exception into a normalized shape. Returns null if no mapping applies.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>Classification result or null.</returns>
    ErrorClassification? Classify(Exception exception);
}
