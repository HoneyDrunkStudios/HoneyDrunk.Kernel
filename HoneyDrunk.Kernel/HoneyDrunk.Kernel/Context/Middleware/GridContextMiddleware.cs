using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context.Mappers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Context.Middleware;

/// <summary>
/// HTTP middleware that maps inbound request headers to a Grid context, establishes an operation context,
/// and populates ambient accessors for downstream services.
/// </summary>
/// <remarks>
/// Responsibilities:
/// 1. Extract correlation / causation / studio / baggage headers.
/// 2. Create a GridContext and set the ambient accessor.
/// 3. Create an OperationContext for request scope.
/// 4. Echo correlation + node id headers to the response for traceability.
/// 5. Constrain header lengths defensively to avoid abuse.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GridContextMiddleware"/> class.
/// </remarks>
/// <param name="next">The next request delegate in the pipeline.</param>
/// <param name="logger">ILogger for diagnostics.</param>
public sealed class GridContextMiddleware(RequestDelegate next, ILogger<GridContextMiddleware> logger)
{
    private const int MaxHeaderLength = 256; // Defensive cap for correlation/causation/studio ids
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GridContextMiddleware> _logger = logger;

    /// <summary>
    /// Invokes the middleware for the given HTTP request, establishing Grid and Operation contexts.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <param name="nodeContext">Process-scoped node context.</param>
    /// <param name="gridAccessor">Ambient grid context accessor.</param>
    /// <param name="opAccessor">Ambient operation context accessor.</param>
    /// <param name="opFactory">Factory for creating operation contexts.</param>
    /// <returns>A task representing the asynchronous pipeline continuation.</returns>
    public async Task InvokeAsync(HttpContext httpContext, INodeContext nodeContext, IGridContextAccessor gridAccessor, IOperationContextAccessor opAccessor, IOperationContextFactory opFactory)
    {
        // Map inbound headers to GridContext.
        var mapper = new HttpContextMapper(nodeContext.NodeId, nodeContext.StudioId, nodeContext.Environment);
        var gridContext = mapper.MapFromHttpContext(httpContext);

        // Defensive truncation (if external systems send very long values, trim to cap).
        gridContext = SanitizeGridContext(gridContext);

        gridAccessor.GridContext = gridContext;

        // Create operation context (scope = request) with metadata.
        var metadata = new Dictionary<string, object?>
        {
            ["http.method"] = httpContext.Request.Method,
            ["http.path"] = httpContext.Request.Path.ToString(),
            ["http.request_id"] = httpContext.TraceIdentifier,
        };
        var operation = opFactory.Create("HttpRequest", metadata);
        opAccessor.Current = operation;

        // Echo correlation + node id for client trace continuity.
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[GridHeaderNames.CorrelationId] = gridContext.CorrelationId;
            httpContext.Response.Headers[GridHeaderNames.NodeId] = nodeContext.NodeId;
            return Task.CompletedTask;
        });

        try
        {
            await _next(httpContext);
            operation.Complete();
        }
        catch (Exception ex)
        {
            operation.Fail("Unhandled exception", ex);
            _logger.LogError(ex, "Unhandled exception during request with correlation {CorrelationId}", gridContext.CorrelationId);
            throw; // Let upstream exception handling middleware produce response.
        }
        finally
        {
            // Clear ambient contexts (avoid leakage to pooled threads in future scenarios).
            opAccessor.Current = null;
            gridAccessor.GridContext = null;
            operation.Dispose();
        }
    }

    private static IGridContext SanitizeGridContext(IGridContext ctx)
    {
        static string Truncate(string value) => value.Length <= MaxHeaderLength ? value : value[..MaxHeaderLength];

        var corr = Truncate(ctx.CorrelationId);
        var cause = ctx.CausationId is not null ? Truncate(ctx.CausationId) : null;
        var node = Truncate(ctx.NodeId);
        var studio = Truncate(ctx.StudioId);
        var env = Truncate(ctx.Environment);

        // Preserve baggage as-is; high cardinality keys should be filtered upstream.
        if (corr == ctx.CorrelationId && cause == ctx.CausationId && node == ctx.NodeId && studio == ctx.StudioId && env == ctx.Environment)
        {
            return ctx; // No change.
        }

        return new GridContext(corr, node, studio, env, cause, ctx.Baggage, ctx.CreatedAtUtc, ctx.Cancellation);
    }
}
