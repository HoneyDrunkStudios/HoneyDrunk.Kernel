// <copyright file="ISecretsSource.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Config;

/// <summary>
/// Provides access to secrets from a secure store.
/// </summary>
public interface ISecretsSource
{
    /// <summary>
    /// Attempts to retrieve a secret value by key.
    /// </summary>
    /// <param name="key">The key identifying the secret.</param>
    /// <param name="value">The secret value if found; otherwise null.</param>
    /// <returns>True if the secret was found; otherwise false.</returns>
    bool TryGetSecret(string key, out string? value);
}
