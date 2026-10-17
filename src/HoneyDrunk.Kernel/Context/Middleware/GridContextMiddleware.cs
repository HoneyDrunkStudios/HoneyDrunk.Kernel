using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Context.Middleware;

/// <summary>
/// HTTP middleware that initializes the scoped GridContext from request headers and establishes an OperationContext.
/// </summary>
/// <remarks>
/// <para>
/// This middleware is responsible for:
/// </para>
/// <list type="number">
/// <item>Extracting correlation/causation/studio/baggage headers from the inbound request.</item>
/// <item>Initializing the DI-scoped GridContext with extracted values.</item>
/// <item>Creating an OperationContext for the request scope.</item>
/// <item>Echoing correlation and node ID headers to the response for traceability.</item>
/// <item>Marking the GridContext as disposed when the request ends.</item>
/// </list>
/// <para>
/// <strong>Important:</strong> This middleware does NOT create a new GridContext. It initializes the
/// existing scoped instance created by DI. This ensures DI, accessor, and OperationContext all see
/// the same GridContext.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GridContextMiddleware"/> class.
/// </remarks>
/// <param name="next">The next request delegate in the pipeline.</param>
/// <param name="logger">ILogger for diagnostics.</param>
public sealed class GridContextMiddleware(RequestDelegate next, ILogger<GridContextMiddleware> logger)
{
    private const int MaxHeaderLength = 256;

    /// <summary>
    /// Invokes the middleware for the given HTTP request.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <returns>A task representing the asynchronous pipeline continuation.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2139:Exceptions should be either logged or rethrown but not both",
        Justification = "Middleware deliberately logs with CorrelationId context (not in any logger scope at this level) then rethrows so the global error handler can produce the response. Dropping the log would lose the per-request correlation attribution.")]
    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Resolve the scoped GridContext from DI - this is THE single instance for this request
        var gridContext = httpContext.RequestServices.GetRequiredService<GridContext>();
        var nodeContext = httpContext.RequestServices.GetRequiredService<INodeContext>();
        var opAccessor = httpContext.RequestServices.GetRequiredService<IOperationContextAccessor>();
        var opFactory = httpContext.RequestServices.GetRequiredService<IOperationContextFactory>();

        // Extract values from headers and initialize the scoped context
        var correlationId = HttpContextExtraction.ExtractCorrelationId(httpContext);
        var causationId = HttpContextExtraction.ExtractHeader(httpContext, GridHeaderNames.CausationId);
        var tenantId = HttpContextExtraction.ExtractHeader(httpContext, GridHeaderNames.TenantId);
        var projectId = HttpContextExtraction.ExtractHeader(httpContext, GridHeaderNames.ProjectId);
        var baggage = HttpContextExtraction.ExtractBaggage(httpContext);

        // Apply defensive truncation
        correlationId = Truncate(correlationId);
        causationId = TruncateNullable(causationId);
        tenantId = TruncateNullable(tenantId);
        projectId = TruncateNullable(projectId);

        if (!TryParseTenantId(tenantId, out var parsedTenantId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsync($"Header {GridHeaderNames.TenantId} must be a valid ULID.");
            gridContext.MarkDisposed();
            return;
        }

        // Initialize the scoped GridContext (this throws if already initialized)
        gridContext.Initialize(
            correlationId: correlationId,
            causationId: causationId,
            tenantId: parsedTenantId,
            projectId: projectId,
            baggage: baggage,
            cancellation: httpContext.RequestAborted);

        // Create operation context (scope = request) with metadata
        var metadata = new Dictionary<string, object?>
        {
            ["http.method"] = httpContext.Request.Method,
            ["http.path"] = httpContext.Request.Path.ToString(),
            ["http.request_id"] = httpContext.TraceIdentifier,
        };
        using var operation = opFactory.Create("HttpRequest", metadata);
        opAccessor.Current = operation;

        // Capture values for response headers BEFORE disposal
        // (OnStarting runs after finally block, so we can't access gridContext there)
        var responseCorrelationId = correlationId;
        var responseNodeId = nodeContext.NodeId;
        var responseTenantId = parsedTenantId;
        var responseProjectId = projectId;

        // Echo correlation + node id for client trace continuity
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[GridHeaderNames.CorrelationId] = responseCorrelationId;
            httpContext.Response.Headers[GridHeaderNames.NodeId] = responseNodeId;

            if (!responseTenantId.IsInternal)
            {
                httpContext.Response.Headers[GridHeaderNames.TenantId] = responseTenantId.ToString();
            }

            if (!string.IsNullOrWhiteSpace(responseProjectId))
            {
                httpContext.Response.Headers[GridHeaderNames.ProjectId] = responseProjectId;
            }

            return Task.CompletedTask;
        });

        try
        {
            await next(httpContext);
            operation.Complete();
        }
        catch (Exception ex)
        {
            operation.Fail("Unhandled exception", ex);
            logger.LogError(ex, "Unhandled exception during request with correlation {CorrelationId}", gridContext.CorrelationId);
            throw;
        }
        finally
        {
            // Clear operation context accessor
            opAccessor.Current = null;

            // Mark the GridContext as disposed to catch fire-and-forget misuse
            gridContext.MarkDisposed();
        }
    }

    private static bool TryParseTenantId(string? value, out TenantId tenantId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            tenantId = TenantId.Internal;
            return true;
        }

        return TenantId.TryParse(value, out tenantId);
    }

    private static string Truncate(string value) =>
        value.Length <= MaxHeaderLength ? value : value[..MaxHeaderLength];

    private static string? TruncateNullable(string? value) =>
        value is not null && value.Length > MaxHeaderLength ? value[..MaxHeaderLength] : value;
}
