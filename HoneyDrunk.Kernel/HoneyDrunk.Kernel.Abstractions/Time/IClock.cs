// <copyright file="IClock.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Time;

/// <summary>
/// Provides access to the current time in a testable manner.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets a high-resolution timestamp for measuring time intervals.
    /// </summary>
    /// <returns>A monotonically increasing timestamp value.</returns>
    long GetTimestamp();
}
