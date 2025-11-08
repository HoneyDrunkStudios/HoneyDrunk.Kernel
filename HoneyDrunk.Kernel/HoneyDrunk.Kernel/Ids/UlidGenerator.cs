// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HoneyDrunk.Kernel.Abstractions.Ids;

namespace HoneyDrunk.Kernel.Ids;

/// <summary>
/// Generates ULID-based identifiers (Universally Unique Lexicographically Sortable Identifier).
/// </summary>
public sealed class UlidGenerator : IIdGenerator
{
    /// <inheritdoc />
    public string NewString() => Ulid.NewUlid().ToString();

    /// <inheritdoc />
    public Guid NewGuid() => Ulid.NewUlid().ToGuid();
}
