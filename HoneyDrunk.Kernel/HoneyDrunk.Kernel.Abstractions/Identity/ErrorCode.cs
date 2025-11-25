namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a structured error code used across the Grid for classification and mapping.
/// </summary>
/// <remarks>
/// Format is segmented code parts separated by dots, enforcing a taxonomy like: "validation.input.missing".
/// Each segment must be lowercase alphanumeric (kebab not allowed inside segment) and 1-32 chars. Max overall length 128.
/// </remarks>
public readonly record struct ErrorCode
{
    private const int MaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorCode"/> struct.
    /// </summary>
    /// <param name="value">The error code value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
    public ErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (!IsValid(value, out var error))
        {
            throw new ArgumentException(error, nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying string value.
    /// </summary>
    public string Value { get; } = string.Empty;

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    /// <param name="code">The error code to convert.</param>
    public static implicit operator string(ErrorCode code) => code.Value;

    /// <summary>
    /// Validates a candidate error code value.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    /// <param name="errorMessage">Detailed error when invalid.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(string? value, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Error code cannot be null or whitespace.";
            return false;
        }

        if (value.Length > MaxLength)
        {
            errorMessage = $"Error code cannot exceed {MaxLength} characters.";
            return false;
        }

        var segments = value.Split('.');
        if (segments.Length == 0)
        {
            errorMessage = "Error code must contain at least one segment.";
            return false;
        }

        foreach (var segment in segments)
        {
            if (segment.Length is < 1 or > 32)
            {
                errorMessage = "Each segment must be between 1 and 32 characters.";
                return false;
            }

            foreach (var ch in segment)
            {
                if (!(ch is >= 'a' and <= 'z') && !(ch is >= '0' and <= '9'))
                {
                    errorMessage = "Segments must be lowercase alphanumeric only.";
                    return false;
                }
            }
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Attempts to parse a string into an <see cref="ErrorCode"/>.
    /// </summary>
    /// <param name="value">Candidate value.</param>
    /// <param name="errorCode">Parsed value when successful.</param>
    /// <returns>True if parsing succeeds; otherwise false.</returns>
    public static bool TryParse(string? value, out ErrorCode errorCode)
    {
        if (IsValid(value, out _))
        {
            errorCode = new ErrorCode(value!);
            return true;
        }

        errorCode = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// Well-known error codes for common failure scenarios.
    /// </summary>
    public static class WellKnown
    {
        /// <summary>Validation failure - input validation failed.</summary>
        public static readonly ErrorCode ValidationInput = new("validation.input");

        /// <summary>Validation failure - business rule violation.</summary>
        public static readonly ErrorCode ValidationBusiness = new("validation.business");

        /// <summary>Authentication failure - user not authenticated.</summary>
        public static readonly ErrorCode AuthenticationFailure = new("authentication.failure");

        /// <summary>Authorization failure - insufficient permissions.</summary>
        public static readonly ErrorCode AuthorizationFailure = new("authorization.failure");

        /// <summary>Dependency failure - external service unavailable.</summary>
        public static readonly ErrorCode DependencyUnavailable = new("dependency.unavailable");

        /// <summary>Dependency failure - external service timeout.</summary>
        public static readonly ErrorCode DependencyTimeout = new("dependency.timeout");

        /// <summary>Resource not found.</summary>
        public static readonly ErrorCode ResourceNotFound = new("resource.notfound");

        /// <summary>Resource conflict - duplicate or constraint violation.</summary>
        public static readonly ErrorCode ResourceConflict = new("resource.conflict");

        /// <summary>Configuration error - missing or invalid configuration.</summary>
        public static readonly ErrorCode ConfigurationInvalid = new("configuration.invalid");

        /// <summary>Internal error - unhandled exception.</summary>
        public static readonly ErrorCode InternalError = new("internal.error");

        /// <summary>Rate limit exceeded.</summary>
        public static readonly ErrorCode RateLimitExceeded = new("ratelimit.exceeded");
    }
}
