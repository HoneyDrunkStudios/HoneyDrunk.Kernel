namespace HoneyDrunk.Kernel.Abstractions.Identity;

/// <summary>
/// Represents a structured error code used across the Grid for classification and mapping.
/// </summary>
/// <remarks>
/// Format is segmented code parts separated by dots, enforcing a taxonomy like: "validation.input.missing".
/// Each segment must be lowercase alphanumeric and hyphens (kebab-case within segments) and 1-32 chars. Max overall length 128.
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
                if (!(ch is >= 'a' and <= 'z') && !(ch is >= '0' and <= '9') && ch != '-')
                {
                    errorMessage = "Segments must be lowercase alphanumeric and hyphens only (kebab-case).";
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
    /// Well-known error codes for common failure scenarios across the Grid.
    /// </summary>
    /// <remarks>
    /// Kernel defines foundational categories that all Nodes extend.
    /// Domain-specific Nodes (Assets, AgentKit, Cyberware, etc.) define their own error codes
    /// following the same namespace pattern (e.g., "asset.too-large", "agent.plan-failed").
    /// </remarks>
    public static class WellKnown
    {
        // === Validation ===

        /// <summary>Validation failure - input validation failed.</summary>
        public static readonly ErrorCode ValidationInput = new("validation.input");

        /// <summary>Validation failure - business rule violation.</summary>
        public static readonly ErrorCode ValidationBusiness = new("validation.business");

        // === Authentication & Authorization ===

        /// <summary>Authentication failure - user not authenticated.</summary>
        public static readonly ErrorCode AuthenticationFailure = new("authentication.failure");

        /// <summary>Authentication failure - token expired.</summary>
        public static readonly ErrorCode AuthenticationTokenExpired = new("authentication.token-expired");

        /// <summary>Authorization failure - insufficient permissions.</summary>
        public static readonly ErrorCode AuthorizationFailure = new("authorization.failure");

        // === Context (Grid-specific) ===

        /// <summary>Context missing - required context (TenantId, CorrelationId, NodeId) not present.</summary>
        public static readonly ErrorCode ContextMissing = new("context.missing");

        /// <summary>Context invalid - context exists but is malformed or cross-tenant.</summary>
        public static readonly ErrorCode ContextInvalid = new("context.invalid");

        /// <summary>Tenant inactive - tenant exists but is disabled or suspended.</summary>
        public static readonly ErrorCode TenantInactive = new("tenant.inactive");

        /// <summary>Project inactive - project exists but is disabled or suspended.</summary>
        public static readonly ErrorCode ProjectInactive = new("project.inactive");

        // === Resource ===

        /// <summary>Resource not found.</summary>
        public static readonly ErrorCode ResourceNotFound = new("resource.notfound");

        /// <summary>Resource conflict - duplicate or constraint violation.</summary>
        public static readonly ErrorCode ResourceConflict = new("resource.conflict");

        // === Operation & State (Distributed system) ===

        /// <summary>State conflict - optimistic concurrency token mismatch.</summary>
        public static readonly ErrorCode StateVersionConflict = new("state.version-conflict");

        /// <summary>Operation replay - idempotent operation already applied.</summary>
        public static readonly ErrorCode OperationIdempotentReplay = new("operation.idempotent-replay");

        /// <summary>Operation timeout - operation exceeded time limit.</summary>
        public static readonly ErrorCode OperationTimeout = new("operation.timeout");

        // === Contract (Transport/Envelope) ===

        /// <summary>Contract invalid - payload doesn't match schema.</summary>
        public static readonly ErrorCode ContractInvalid = new("contract.invalid");

        /// <summary>Contract version unsupported - client using unsupported schema version.</summary>
        public static readonly ErrorCode ContractUnsupportedVersion = new("contract.unsupported-version");

        /// <summary>Contract missing field - required envelope field not present.</summary>
        public static readonly ErrorCode ContractMissingField = new("contract.missing-field");

        // === Feature & Quota (Runtime gating) ===

        /// <summary>Feature disabled - feature flag is off globally or per-tenant.</summary>
        public static readonly ErrorCode FeatureDisabled = new("feature.disabled");

        /// <summary>Feature not allowed - feature requires higher tier or permission.</summary>
        public static readonly ErrorCode FeatureNotAllowed = new("feature.not-allowed");

        /// <summary>Quota exceeded - tenant hit limits (events, storage, API calls).</summary>
        public static readonly ErrorCode QuotaExceeded = new("quota.exceeded");

        // === Dependency ===

        /// <summary>Dependency unavailable - external service unavailable.</summary>
        public static readonly ErrorCode DependencyUnavailable = new("dependency.unavailable");

        /// <summary>Dependency timeout - external service timeout.</summary>
        public static readonly ErrorCode DependencyTimeout = new("dependency.timeout");

        // === Configuration ===

        /// <summary>Configuration invalid - missing or invalid configuration.</summary>
        public static readonly ErrorCode ConfigurationInvalid = new("configuration.invalid");

        // === System ===

        /// <summary>Internal error - unhandled exception.</summary>
        public static readonly ErrorCode InternalError = new("internal.error");

        /// <summary>Service unavailable - Node is not ready or shutting down.</summary>
        public static readonly ErrorCode ServiceUnavailable = new("service.unavailable");

        /// <summary>Rate limit exceeded.</summary>
        public static readonly ErrorCode RateLimitExceeded = new("rate-limit.exceeded");
    }
}
