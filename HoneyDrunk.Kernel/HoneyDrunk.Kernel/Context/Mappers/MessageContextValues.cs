// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Values extracted from message metadata for GridContext initialization.
/// </summary>
/// <param name="CorrelationId">The correlation identifier (may be null if not present in metadata).</param>
/// <param name="CausationId">Optional causation identifier.</param>
/// <param name="TenantId">Optional tenant identifier.</param>
/// <param name="ProjectId">Optional project identifier.</param>
/// <param name="Baggage">Baggage dictionary (may be empty).</param>
public sealed record MessageContextValues(
    string? CorrelationId,
    string? CausationId,
    string? TenantId,
    string? ProjectId,
    IReadOnlyDictionary<string, string> Baggage);
