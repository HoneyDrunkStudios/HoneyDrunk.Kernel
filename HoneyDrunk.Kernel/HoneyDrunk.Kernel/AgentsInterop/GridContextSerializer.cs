using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Serializes GridContext for agent consumption.
/// </summary>
/// <remarks>
/// Agents receive a scoped view of GridContext based on their permissions.
/// This serializer creates JSON representations that agents can parse and use.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S1118:Utility classes should not have public constructors",
    Justification = "Public sealed class shape preserved for HoneyDrunk.Kernel 0.7.x binary compatibility. Converting to static class is a published-API break that requires a coordinated minor-version bump across all cross-node consumers; deferred to a focused initiative.")]
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

            // Required fields. TryGetStringProperty returns null for both
            // missing properties and non-string JSON shapes (number, bool,
            // array, object), so the string.IsNullOrEmpty checks below treat
            // every bad-shape case as "field absent" — matching Deserialize's
            // documented "returns null on bad input" contract.
            var correlationId = TryGetStringProperty(root, "correlationId");
            var nodeId = TryGetStringProperty(root, "nodeId");
            var studioId = TryGetStringProperty(root, "studioId");
            var environment = TryGetStringProperty(root, "environment");

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

    // Returns the property's string value if present AND the JSON shape is a string;
    // otherwise null. Guards Deserialize's documented "returns null on bad input" contract
    // against unexpected JSON kinds (e.g., a number where a string was expected) — without
    // this check, JsonElement.GetString() throws InvalidOperationException which would
    // escape Deserialize's JsonException-only catch.
    private static string? TryGetStringProperty(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static bool TryGetTenantId(JsonElement root, out TenantId tenantId)
    {
        tenantId = TenantId.Internal;
        if (!root.TryGetProperty("tenantId", out var element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) || TenantId.TryParse(value, out tenantId);
    }

    private static Dictionary<string, string> ExtractBaggage(JsonElement root)
    {
        var baggage = new Dictionary<string, string>();
        if (!root.TryGetProperty("baggage", out var baggageElement) || baggageElement.ValueKind != JsonValueKind.Object)
        {
            return baggage;
        }

        foreach (var prop in baggageElement.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            // ValueKind == String guarantees GetString() is non-null.
            baggage[prop.Name] = prop.Value.GetString()!;
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
