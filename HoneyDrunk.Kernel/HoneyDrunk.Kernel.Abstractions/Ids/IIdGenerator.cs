// <copyright file="IIdGenerator.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Ids;

/// <summary>
/// Generates unique identifiers for correlation and tracing.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier as a string.
    /// </summary>
    /// <returns>A unique string identifier.</returns>
    string NewString();

    /// <summary>
    /// Generates a new globally unique identifier.
    /// </summary>
    /// <returns>A new GUID.</returns>
    Guid NewGuid();
}
