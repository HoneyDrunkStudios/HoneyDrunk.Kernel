namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Represents a normalized classification of an exception suitable for transport/ProblemDetails mapping.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ErrorClassification"/> class.
/// </remarks>
/// <param name="statusCode">Suggested transport status code (HTTP or analogous).</param>
/// <param name="title">Human-readable short title.</param>
/// <param name="errorCode">Structured error code (taxonomy) if available.</param>
/// <param name="typeUri">Optional URI pointing to documentation.</param>
public sealed class ErrorClassification(int statusCode, string title, string? errorCode = null, string? typeUri = null)
{
    /// <summary>
    /// Gets the suggested transport status code (HTTP or analogous).
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// Gets the human-readable short title.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Gets the structured error code (taxonomy) if available.
    /// </summary>
    public string? ErrorCode { get; } = errorCode;

    /// <summary>
    /// Gets the optional URI pointing to documentation for this error.
    /// </summary>
    public string? TypeUri { get; } = typeUri;
}
