// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Implementation of <see cref="IGridContextAccessor"/> that provides ambient access to the DI-scoped GridContext.
/// </summary>
/// <remarks>
/// <para>
/// This accessor retrieves the GridContext from the current HTTP request's service scope,
/// ensuring it always returns the same instance that DI resolves. It does NOT maintain
/// an independent AsyncLocal store.
/// </para>
/// <para>
/// For non-HTTP scenarios (background services, jobs), use <see cref="ScopedGridContextAccessor"/>
/// which uses AsyncLocal to track the current scope's context.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GridContextAccessor"/> class.
/// </remarks>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
public sealed class GridContextAccessor(IHttpContextAccessor httpContextAccessor) : IGridContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc />
    public IGridContext GridContext
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException(
                    "GridContext cannot be accessed outside of an HTTP request scope. " +
                    "For background services or jobs, create context explicitly using mappers or factories.");

            // Resolve from request services - this is the same scoped instance DI created
            if (httpContext.RequestServices.GetService(typeof(IGridContext)) is not IGridContext context)
            {
                throw new InvalidOperationException(
                    "GridContext is not registered in the service container. " +
                    "Ensure AddHoneyDrunkNode() was called during application startup.");
            }

            return context;
        }
    }
}
