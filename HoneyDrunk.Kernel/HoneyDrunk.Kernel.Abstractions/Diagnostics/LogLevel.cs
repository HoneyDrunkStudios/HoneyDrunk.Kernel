// <copyright file="LogLevel.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Diagnostics;

/// <summary>
/// Defines log severity levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Verbose debugging information.
    /// </summary>
    Trace,

    /// <summary>
    /// Debugging information.
    /// </summary>
    Debug,

    /// <summary>
    /// Informational messages.
    /// </summary>
    Information,

    /// <summary>
    /// Warning messages for potentially harmful situations.
    /// </summary>
    Warning,

    /// <summary>
    /// Error messages for failures.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error messages for severe failures.
    /// </summary>
    Critical,
}
