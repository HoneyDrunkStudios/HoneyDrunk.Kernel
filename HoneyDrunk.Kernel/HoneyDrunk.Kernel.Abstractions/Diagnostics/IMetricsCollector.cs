// <copyright file="IMetricsCollector.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Diagnostics;

/// <summary>
/// Provides methods for recording metrics in a testable manner.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Records a counter metric that can only increase.
    /// </summary>
    /// <param name="name">The name of the counter metric.</param>
    /// <param name="value">The value to add to the counter (default: 1).</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions.</param>
    void RecordCounter(string name, long value = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a histogram metric for value distribution analysis.
    /// </summary>
    /// <param name="name">The name of the histogram metric.</param>
    /// <param name="value">The value to record in the histogram.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions.</param>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a gauge metric that represents a current value that can go up or down.
    /// </summary>
    /// <param name="name">The name of the gauge metric.</param>
    /// <param name="value">The current value of the gauge.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions.</param>
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);
}
