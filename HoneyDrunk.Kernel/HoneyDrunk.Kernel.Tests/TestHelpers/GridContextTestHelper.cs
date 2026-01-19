// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.TestHelpers;

/// <summary>
/// Helper methods for creating GridContext instances in tests.
/// </summary>
/// <remarks>
/// Since GridContext now requires two-phase initialization (construct + Initialize()),
/// this helper provides convenience methods that do both steps.
/// </remarks>
public static class GridContextTestHelper
{
    /// <summary>
    /// Creates and initializes a GridContext with the specified values.
    /// </summary>
    /// <param name="correlationId">The correlation ID for distributed tracing.</param>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="studioId">The studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="causationId">Optional causation ID.</param>
    /// <param name="tenantId">Optional tenant ID.</param>
    /// <param name="projectId">Optional project ID.</param>
    /// <param name="baggage">Optional baggage dictionary.</param>
    /// <param name="cancellation">Optional cancellation token.</param>
    /// <returns>A fully initialized GridContext instance.</returns>
    public static GridContext CreateInitialized(
        string correlationId,
        string nodeId,
        string studioId,
        string environment,
        string? causationId = null,
        string? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        CancellationToken cancellation = default)
    {
        var context = new GridContext(nodeId, studioId, environment);
        context.Initialize(
            correlationId: correlationId,
            causationId: causationId,
            tenantId: tenantId,
            projectId: projectId,
            baggage: baggage,
            cancellation: cancellation);
        return context;
    }

    /// <summary>
    /// Creates and initializes a GridContext with default test values.
    /// </summary>
    /// <param name="correlationId">The correlation ID for distributed tracing.</param>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="studioId">The studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <returns>A fully initialized GridContext instance with default test values.</returns>
    public static GridContext CreateDefault(
        string correlationId = "test-correlation-id",
        string nodeId = "test-node",
        string studioId = "test-studio",
        string environment = "test-env")
    {
        return CreateInitialized(correlationId, nodeId, studioId, environment);
    }

    /// <summary>
    /// Creates an uninitialized GridContext (for testing initialization behavior).
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="studioId">The studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <returns>An uninitialized GridContext instance.</returns>
    public static GridContext CreateUninitialized(
        string nodeId = "test-node",
        string studioId = "test-studio",
        string environment = "test-env")
    {
        return new GridContext(nodeId, studioId, environment);
    }
}
