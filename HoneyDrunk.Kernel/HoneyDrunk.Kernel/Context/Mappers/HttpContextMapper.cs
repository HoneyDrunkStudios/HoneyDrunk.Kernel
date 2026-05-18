using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Maps HTTP request headers to GridContext initialization parameters.
/// </summary>
/// <remarks>
/// <para>
/// This mapper extracts correlation, causation, tenant, project, and baggage values from HTTP headers.
/// It is used by middleware to initialize the scoped GridContext.
/// </para>
/// <para>
/// Standard headers:
/// </para>
/// <list type="bullet">
/// <item>X-Correlation-Id or traceparent → CorrelationId</item>
/// <item>X-Causation-Id → CausationId</item>
/// <item>X-Tenant-Id → TenantId (identity only)</item>
/// <item>X-Project-Id → ProjectId (identity only)</item>
/// <item>baggage, X-Baggage-* → Baggage</item>
/// </list>
/// </remarks>
public sealed class HttpContextMapper
{
    /// <summary>
    /// Extracts GridContext initialization values from an HTTP request.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>Values needed to initialize a GridContext.</returns>
    public static GridContextInitValues ExtractFromHttpContext(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = HttpContextExtraction.ExtractCorrelationId(httpContext);
        var causationId = HttpContextExtraction.ExtractHeader(httpContext, GridHeaderNames.CausationId);
        var tenantId = ParseTenantIdOrInternal(HttpContextExtraction.ExtractHeader(httpContext, GridHeaderNames.TenantId));
        var projectId = HttpContextExtraction.ExtractHeader(httpContext, GridHeaderNames.ProjectId);
        var baggage = HttpContextExtraction.ExtractBaggage(httpContext);

        return new GridContextInitValues(
            correlationId,
            causationId,
            tenantId,
            projectId,
            baggage,
            httpContext.RequestAborted);
    }

    /// <summary>
    /// Initializes an existing GridContext from HTTP request headers.
    /// </summary>
    /// <param name="context">The GridContext to initialize.</param>
    /// <param name="httpContext">The HTTP context containing headers.</param>
    public static void InitializeFromHttpContext(GridContext context, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpContext);

        var values = ExtractFromHttpContext(httpContext);
        context.Initialize(
            values.CorrelationId,
            values.CausationId,
            values.TenantId,
            values.ProjectId,
            values.Baggage,
            values.Cancellation);
    }

    private static TenantId ParseTenantIdOrInternal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantId.Internal;
        }

        if (TenantId.TryParse(value, out var tenantId))
        {
            return tenantId;
        }

        throw new FormatException($"Header {GridHeaderNames.TenantId} must be a valid ULID.");
    }
}
