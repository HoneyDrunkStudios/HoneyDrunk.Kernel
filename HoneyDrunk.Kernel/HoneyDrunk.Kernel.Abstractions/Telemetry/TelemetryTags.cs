namespace HoneyDrunk.Kernel.Abstractions.Telemetry;

/// <summary>
/// Standard telemetry tag names for HoneyDrunk.OS Grid-wide observability.
/// </summary>
/// <remarks>
/// These constants define the semantic standard for tagging metrics, traces, and logs
/// across the entire Grid. All Nodes should use these tag names for consistency.
/// This enables unified querying, filtering, and correlation in observability backends.
/// Follows OpenTelemetry semantic conventions with "hd." prefix for HoneyDrunk-specific tags.
/// </remarks>
public static class TelemetryTags
{
    /// <summary>
    /// Tag for the correlation ID that groups related operations.
    /// Value: "hd.correlation_id".
    /// </summary>
    public const string CorrelationId = "hd.correlation_id";

    /// <summary>
    /// Tag for the causation ID indicating which operation triggered this one.
    /// Value: "hd.causation_id".
    /// </summary>
    public const string CausationId = "hd.causation_id";

    /// <summary>
    /// Tag for the Node identifier.
    /// Value: "hd.node_id"
    /// Example: "payment-node", "notification-node".
    /// </summary>
    public const string NodeId = "hd.node_id";

    /// <summary>
    /// Tag for the Node version.
    /// Value: "hd.node_version"
    /// Example: "1.0.0", "2.1.3-beta".
    /// </summary>
    public const string NodeVersion = "hd.node_version";

    /// <summary>
    /// Tag for the Studio identifier.
    /// Value: "hd.studio_id"
    /// Example: "honeycomb", "staging", "dev-alice".
    /// </summary>
    public const string StudioId = "hd.studio_id";

    /// <summary>
    /// Tag for the Tenant identifier.
    /// Value: "hd.tenant_id".
    /// </summary>
    public const string TenantId = "hd.tenant_id";

    /// <summary>
    /// Tag for the Project identifier.
    /// Value: "hd.project_id".
    /// </summary>
    public const string ProjectId = "hd.project_id";

    /// <summary>
    /// Tag for the environment name.
    /// Value: "hd.environment"
    /// Example: "production", "staging", "development".
    /// </summary>
    public const string Environment = "hd.environment";

    /// <summary>
    /// Tag for the operation name/type.
    /// Value: "hd.operation"
    /// Example: "ProcessPayment", "HandleWebhook", "SyncInventory".
    /// </summary>
    public const string Operation = "hd.operation";

    /// <summary>
    /// Tag for operation outcome (success/failure).
    /// Value: "hd.outcome"
    /// Example: "success", "failure", "timeout".
    /// </summary>
    public const string Outcome = "hd.outcome";

    /// <summary>
    /// Tag for the Node lifecycle stage.
    /// Value: "hd.lifecycle_stage"
    /// Example: "starting", "running", "stopping".
    /// </summary>
    public const string LifecycleStage = "hd.lifecycle_stage";

    /// <summary>
    /// Tag for the machine/host name.
    /// Value: "hd.machine_name".
    /// </summary>
    public const string MachineName = "hd.machine_name";

    /// <summary>
    /// Tag for the process ID.
    /// Value: "hd.process_id".
    /// </summary>
    public const string ProcessId = "hd.process_id";

    /// <summary>
    /// Tag for error type/category.
    /// Value: "hd.error_type"
    /// Example: "validation", "timeout", "dependency_failure".
    /// </summary>
    public const string ErrorType = "hd.error_type";

    /// <summary>
    /// Tag for error message.
    /// Value: "hd.error_message".
    /// </summary>
    public const string ErrorMessage = "hd.error_message";

    /// <summary>
    /// Tag for request/message source.
    /// Value: "hd.source"
    /// Example: "http", "queue", "timer".
    /// </summary>
    public const string Source = "hd.source";

    /// <summary>
    /// Tag for target/destination.
    /// Value: "hd.target"
    /// Example: "database", "external_api", "message_queue".
    /// </summary>
    public const string Target = "hd.target";

    /// <summary>
    /// Tag for duration/latency in milliseconds.
    /// Value: "hd.duration_ms".
    /// </summary>
    public const string DurationMs = "hd.duration_ms";

    /// <summary>
    /// Tag for the caller identity (user, agent, service).
    /// Value: "hd.caller_id".
    /// </summary>
    public const string CallerId = "hd.caller_id";

    /// <summary>
    /// Tag for the caller type.
    /// Value: "hd.caller_type"
    /// Example: "user", "agent", "service", "anonymous".
    /// </summary>
    public const string CallerType = "hd.caller_type";
}
