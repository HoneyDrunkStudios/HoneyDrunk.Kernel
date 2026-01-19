// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Values extracted from HTTP context for initializing a GridContext.
/// </summary>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="CausationId">Optional causation identifier.</param>
/// <param name="TenantId">Optional tenant identifier.</param>
/// <param name="ProjectId">Optional project identifier.</param>
/// <param name="Baggage">Extracted baggage key-value pairs.</param>
/// <param name="Cancellation">Cancellation token from request.</param>
public readonly record struct GridContextInitValues(
    string CorrelationId,
    string? CausationId,
    string? TenantId,
    string? ProjectId,
    IReadOnlyDictionary<string, string> Baggage,
    CancellationToken Cancellation);
