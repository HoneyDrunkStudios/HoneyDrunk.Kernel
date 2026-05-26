using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Initializes GridContext from message metadata.
/// </summary>
/// <remarks>
/// <para>
/// Used for message queue consumers, event handlers, and pub/sub scenarios.
/// Extracts correlation/causation from message metadata/headers.
/// </para>
/// <para>
/// This class provides static methods that initialize an existing scoped GridContext
/// rather than creating new instances. The GridContext is owned by DI and should be
/// resolved from the current scope before calling these methods.
/// </para>
/// </remarks>
public static class MessagingContextMapper
{
    /// <summary>
    /// Initializes a GridContext from message metadata.
    /// </summary>
    /// <param name="context">The scoped GridContext to initialize.</param>
    /// <param name="metadata">Message metadata/headers.</param>
    /// <param name="cancellationToken">Cancellation token for message processing.</param>
    public static void InitializeFromMessage(
        GridContext context,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(metadata);

        var values = ExtractFromMessage(metadata);

        context.Initialize(
            correlationId: values.CorrelationId ?? Ulid.NewUlid().ToString(),
            causationId: values.CausationId,
            tenantId: values.TenantId,
            projectId: values.ProjectId,
            baggage: values.Baggage,
            cancellation: cancellationToken);
    }

    /// <summary>
    /// Extracts context initialization values from message metadata.
    /// </summary>
    /// <param name="metadata">Message metadata/headers.</param>
    /// <returns>Extracted values for GridContext initialization. CorrelationId may be null if not present in metadata.</returns>
    /// <remarks>
    /// This is a pure extraction method - it returns exactly what's in the metadata.
    /// If no correlation ID is found, it returns null (unlike <see cref="InitializeFromMessage"/>
    /// which generates a new one).
    /// </remarks>
    public static MessageContextValues ExtractFromMessage(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var correlationId = ResolveMetadata(metadata, "CorrelationId", "correlation-id", "X-Correlation-Id");
        var causationId = ResolveMetadata(metadata, "CausationId", "causation-id", "X-Causation-Id");
        var tenantId = ParseTenantIdOrNull(
            ResolveMetadata(metadata, "TenantId", "tenant-id", GridHeaderNames.TenantId));
        var projectId = ResolveMetadata(metadata, "ProjectId", "project-id", "X-Project-Id");

        // Extract baggage (keys starting with "baggage-" or "Baggage-")
        var baggage = metadata
            .Where(kvp => kvp.Key.StartsWith("baggage-", StringComparison.OrdinalIgnoreCase)
                       || kvp.Key.StartsWith("Baggage-", StringComparison.Ordinal))
            .ToDictionary(
                kvp => kvp.Key[8..], // Remove "baggage-" prefix
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

        return new MessageContextValues(
            CorrelationId: correlationId,
            CausationId: causationId,
            TenantId: tenantId,
            ProjectId: projectId,
            Baggage: baggage);
    }

    private static string? ResolveMetadata(IReadOnlyDictionary<string, string> metadata, params string[] keys) =>
        keys.Where(metadata.ContainsKey).Select(k => metadata[k]).FirstOrDefault();

    private static TenantId? ParseTenantIdOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TenantId.TryParse(value, out var tenantId))
        {
            return tenantId;
        }

        throw new FormatException($"Metadata {GridHeaderNames.TenantId} must be a valid ULID.");
    }
}
