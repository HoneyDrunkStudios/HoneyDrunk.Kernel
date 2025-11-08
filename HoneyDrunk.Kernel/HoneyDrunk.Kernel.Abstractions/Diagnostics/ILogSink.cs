// <copyright file="ILogSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Diagnostics;

/// <summary>
/// Minimal structured logging contract for capturing log entries.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="level">The severity level of the log entry.</param>
    /// <param name="messageTemplate">The message template with placeholders.</param>
    /// <param name="properties">Named properties for the message template.</param>
    /// <param name="exception">Optional exception associated with the log entry.</param>
    void Write(LogLevel level, string messageTemplate, IReadOnlyDictionary<string, object?> properties, Exception? exception = null);
}
