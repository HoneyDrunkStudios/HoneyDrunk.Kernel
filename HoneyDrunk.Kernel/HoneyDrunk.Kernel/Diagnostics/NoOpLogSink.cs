// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HoneyDrunk.Kernel.Abstractions.Diagnostics;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// No-op implementation of ILogSink that discards all log entries.
/// </summary>
public sealed class NoOpLogSink : ILogSink
{
    /// <inheritdoc />
    public void Write(LogLevel level, string messageTemplate, IReadOnlyDictionary<string, object?> properties, Exception? exception = null)
    {
    }
}
