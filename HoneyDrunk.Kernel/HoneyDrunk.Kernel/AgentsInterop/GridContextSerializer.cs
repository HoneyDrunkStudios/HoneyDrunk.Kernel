using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using System.Text.Json;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Serializes GridContext for agent consumption.
/// </summary>
/// <remarks>
/// Agents receive a scoped view of GridContext based on their permissions.
/// This serializer creates JSON representations that agents can parse and use.
/// </remarks>
public static class GridContextSerializer
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
        ArgumentNullException.ThrowIfNull(context);

        var data = new
        {
            correlationId = context.CorrelationId,
            causationId = context.CausationId,
            nodeId = context.NodeId,
            studioId = context.StudioId,
            environment = context.Environment,
            tenantId = context.TenantId.ToString(),
            projectId = context.ProjectId,
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
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

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

            if (string.IsNullOrEmpty(correlationId) ||
                string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(studioId) ||
                string.IsNullOrEmpty(environment))
            {
                return null;
            }

            var causationId = TryGetStringProperty(root, "causationId");

            if (!TryGetTenantId(root, out var tenantId))
            {
                return null;
            }

            var projectId = TryGetStringProperty(root, "projectId");
            var baggage = ExtractBaggage(root);

            // Create and initialize a new GridContext for deserialization scenarios
            // This is used when receiving context from external sources (e.g., agent results)
            var gridContext = new Context.GridContext(
                nodeId: nodeId,
                studioId: studioId,
                environment: environment);

            gridContext.Initialize(
                correlationId: correlationId,
                causationId: causationId,
                tenantId: tenantId,
                projectId: projectId,
                baggage: baggage);

            return gridContext;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryGetStringProperty(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) ? element.GetString() : null;

    private static bool TryGetTenantId(JsonElement root, out TenantId tenantId)
    {
        tenantId = TenantId.Internal;
        if (!root.TryGetProperty("tenantId", out var element))
        {
            return true;
        }

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) || TenantId.TryParse(value, out tenantId);
    }

    private static Dictionary<string, string> ExtractBaggage(JsonElement root)
    {
        var baggage = new Dictionary<string, string>();
        if (!root.TryGetProperty("baggage", out var baggageElement))
        {
            return baggage;
        }

        foreach (var prop in baggageElement.EnumerateObject())
        {
            var value = prop.Value.GetString();
            if (value != null)
            {
                baggage[prop.Name] = value;
            }
        }

        return baggage;
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
