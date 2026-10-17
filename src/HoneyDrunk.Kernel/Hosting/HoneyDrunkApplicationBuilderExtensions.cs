using HoneyDrunk.Kernel.Context.Middleware;
using Microsoft.AspNetCore.Builder;

namespace HoneyDrunk.Kernel.Hosting;

/// <summary>
/// Extension methods for registering HoneyDrunk middleware in the ASP.NET Core pipeline.
/// </summary>
public static class HoneyDrunkApplicationBuilderExtensions
{
    /// <summary>
    /// Adds GridContext middleware to extract Grid identity from HTTP headers and establish operation context.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// This middleware should be registered early in the pipeline to ensure GridContext is available
    /// for all downstream middleware and handlers. It extracts correlation/causation/studio headers,
    /// creates a GridContext, establishes an OperationContext for the request, and echoes
    /// correlation + node ID headers to the response.
    /// </remarks>
    public static IApplicationBuilder UseGridContext(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<GridContextMiddleware>();
    }
}
