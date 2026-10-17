namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Provides access to Node manifests from various sources.
/// </summary>
/// <remarks>
/// Manifest sources enable flexible manifest storage and retrieval:
/// - Embedded resources in the Node binary.
/// - Configuration files (JSON, YAML, TOML).
/// - Service registry (Consul, etcd, Kubernetes ConfigMaps).
/// - Remote APIs or databases.
/// </remarks>
public interface INodeManifestSource
{
    /// <summary>
    /// Gets the name/type of this manifest source.
    /// Examples: "embedded", "file-system", "service-registry", "api".
    /// </summary>
    string SourceType { get; }

    /// <summary>
    /// Attempts to load a manifest for the specified Node.
    /// </summary>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if manifest was found and loaded; otherwise false.</returns>
    Task<(bool success, INodeManifest? manifest)> TryLoadManifestAsync(
        string nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to save/update a manifest.
    /// </summary>
    /// <param name="manifest">The manifest to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if save succeeded; otherwise false.</returns>
    Task<bool> TrySaveManifestAsync(
        INodeManifest manifest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available manifests from this source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of Node IDs that have manifests.</returns>
    Task<IReadOnlyList<string>> ListManifestsAsync(CancellationToken cancellationToken = default);
}
