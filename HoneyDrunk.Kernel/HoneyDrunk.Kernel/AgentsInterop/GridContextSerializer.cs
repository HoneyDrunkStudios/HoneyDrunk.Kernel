using HoneyDrunk.Kernel.Abstractions.Context;
using System.Text.Json;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Serializes GridContext for agent consumption.
/// </summary>
/// <remarks>
/// Agents receive a scoped view of GridContext based on their permissions.
/// This serializer creates JSON representations that agents can parse and use.
/// </remarks>
public sealed class GridContextSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a GridContext to JSON.
    /// </summary>
    /// <param name="context">The GridContext to serialize.</param>
    /// <param name="includeFullBaggage">Whether to include all baggage (default: false for security).</param>
    /// <returns>A JSON string representing the GridContext.</returns>
    public static string Serialize(IGridContext context, bool includeFullBaggage = false)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var data = new
        {
            correlationId = context.CorrelationId,
            causationId = context.CausationId,
            nodeId = context.NodeId,
            studioId = context.StudioId,
            environment = context.Environment,
            createdAtUtc = context.CreatedAtUtc,
            baggage = includeFullBaggage ? context.Baggage : FilterSafeBaggage(context.Baggage),
        };

        return JsonSerializer.Serialize(data, _jsonOptions);
    }

    /// <summary>
    /// Deserializes GridContext from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized GridContext, or null if deserialization fails.</returns>
    public static IGridContext? Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Safely try to get required properties
            if (!root.TryGetProperty("correlationId", out var correlationIdElement) ||
                !root.TryGetProperty("nodeId", out var nodeIdElement) ||
                !root.TryGetProperty("studioId", out var studioIdElement) ||
                !root.TryGetProperty("environment", out var environmentElement))
            {
                return null;
            }

            var correlationId = correlationIdElement.GetString();
            var nodeId = nodeIdElement.GetString();
            var studioId = studioIdElement.GetString();
            var environment = environmentElement.GetString();

            if (string.IsNullOrEmpty(correlationId) || string.IsNullOrEmpty(nodeId) ||
                string.IsNullOrEmpty(studioId) || string.IsNullOrEmpty(environment))
            {
                return null;
            }

            string? causationId = null;
            if (root.TryGetProperty("causationId", out var causationElement))
            {
                causationId = causationElement.GetString();
            }

            var baggage = new Dictionary<string, string>();
            if (root.TryGetProperty("baggage", out var baggageElement))
            {
                foreach (var prop in baggageElement.EnumerateObject())
                {
                    var value = prop.Value.GetString();
                    if (value != null)
                    {
                        baggage[prop.Name] = value;
                    }
                }
            }

            DateTimeOffset? createdAtUtc = null;
            if (root.TryGetProperty("createdAtUtc", out var createdAtElement))
            {
                createdAtUtc = createdAtElement.GetDateTimeOffset();
            }

            return new Context.GridContext(
                correlationId: correlationId,
                nodeId: nodeId,
                studioId: studioId,
                environment: environment,
                causationId: causationId,
                baggage: baggage,
                createdAtUtc: createdAtUtc);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Filters baggage to only include non-sensitive keys.
    /// </summary>
    private static Dictionary<string, string> FilterSafeBaggage(IReadOnlyDictionary<string, string> baggage)
    {
        // Filter out keys that might contain sensitive data
        var filtered = new Dictionary<string, string>();

        foreach (var (key, value) in baggage)
        {
            var lowerKey = key.ToLowerInvariant();

            // Skip keys that might contain secrets
            if (lowerKey.Contains("secret") ||
                lowerKey.Contains("password") ||
                lowerKey.Contains("token") ||
                lowerKey.Contains("key") ||
                lowerKey.Contains("credential"))
            {
                continue;
            }

            filtered[key] = value;
        }

        return filtered;
    }
}
