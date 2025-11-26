using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Maps message/event context to GridContext.
/// </summary>
/// <remarks>
/// Used for message queue consumers, event handlers, and pub/sub scenarios.
/// Extracts correlation/causation from message metadata/headers.
/// </remarks>
public sealed class MessagingContextMapper
{
    private readonly string _nodeId;
    private readonly string _studioId;
    private readonly string _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingContextMapper"/> class.
    /// </summary>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    public MessagingContextMapper(string nodeId, string studioId, string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId, nameof(nodeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId, nameof(studioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(environment, nameof(environment));

        _nodeId = nodeId;
        _studioId = studioId;
        _environment = environment;
    }

    /// <summary>
    /// Creates a GridContext from message metadata.
    /// </summary>
    /// <param name="metadata">Message metadata/headers.</param>
    /// <param name="cancellationToken">Cancellation token for message processing.</param>
    /// <returns>A GridContext populated from message metadata.</returns>
    public IGridContext MapFromMessageMetadata(
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));

        var correlationId = GetMetadata(metadata, "CorrelationId")
            ?? GetMetadata(metadata, "correlation-id")
            ?? GetMetadata(metadata, "X-Correlation-Id")
            ?? Ulid.NewUlid().ToString();

        var operationId = Ulid.NewUlid().ToString(); // New span for this message handler

        var causationId = GetMetadata(metadata, "CausationId")
            ?? GetMetadata(metadata, "causation-id")
            ?? GetMetadata(metadata, "X-Causation-Id");

        var studioId = GetMetadata(metadata, "StudioId")
            ?? GetMetadata(metadata, "studio-id")
            ?? GetMetadata(metadata, "X-Studio-Id")
            ?? _studioId;

        // Extract baggage (keys starting with "baggage-" or "Baggage-")
        var baggage = metadata
            .Where(kvp => kvp.Key.StartsWith("baggage-", StringComparison.OrdinalIgnoreCase)
                       || kvp.Key.StartsWith("Baggage-", StringComparison.Ordinal))
            .ToDictionary(
                kvp => kvp.Key[8..], // Remove "baggage-" prefix
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

        return new GridContext(
            correlationId: correlationId,
            operationId: operationId,
            nodeId: _nodeId,
            studioId: studioId,
            environment: _environment,
            causationId: causationId,
            baggage: baggage,
            cancellation: cancellationToken);
    }

    private static string? GetMetadata(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) ? value : null;
    }
}
