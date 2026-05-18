using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Context;

internal static class HttpContextExtraction
{
    public static string ExtractCorrelationId(HttpContext httpContext)
    {
        var correlationId = ExtractHeader(httpContext, GridHeaderNames.CorrelationId);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        var traceParent = ExtractHeader(httpContext, GridHeaderNames.TraceParent);
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            var parts = traceParent.Split('-');
            if (parts.Length >= 2)
            {
                return parts[1];
            }
        }

        return Ulid.NewUlid().ToString();
    }

    public static string? ExtractHeader(HttpContext httpContext, string headerName)
    {
        if (httpContext.Request.Headers.TryGetValue(headerName, out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }

    public static Dictionary<string, string> ExtractBaggage(HttpContext httpContext)
    {
        var baggage = new Dictionary<string, string>();

        if (httpContext.Request.Headers.TryGetValue(GridHeaderNames.Baggage, out var baggageHeader))
        {
            var baggageString = baggageHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(baggageString))
            {
                var items = baggageString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(ParseBaggageItem)
                    .Where(static parsed => parsed.HasValue)
                    .Select(static parsed => parsed.GetValueOrDefault());

                foreach (var (key, value) in items)
                {
                    baggage[key] = value;
                }
            }
        }

        foreach (var header in httpContext.Request.Headers.Where(static header => header.Key.StartsWith(GridHeaderNames.BaggagePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var key = header.Key[GridHeaderNames.BaggagePrefix.Length..];
            var value = header.Value.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                baggage[key] = value;
            }
        }

        return baggage;
    }

    private static (string key, string value)? ParseBaggageItem(string item)
    {
        var parts = item.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var keyValue = parts[0].Split('=', 2, StringSplitOptions.TrimEntries);
        if (keyValue.Length != 2 || string.IsNullOrWhiteSpace(keyValue[0]) || string.IsNullOrWhiteSpace(keyValue[1]))
        {
            return null;
        }

        return (keyValue[0].Trim(), Uri.UnescapeDataString(keyValue[1].Trim()));
    }
}
