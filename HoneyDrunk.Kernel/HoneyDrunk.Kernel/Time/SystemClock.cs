// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Time;

namespace HoneyDrunk.Kernel.Time;

/// <summary>
/// System implementation of clock using the system's current time.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public long GetTimestamp() => Stopwatch.GetTimestamp();
}
