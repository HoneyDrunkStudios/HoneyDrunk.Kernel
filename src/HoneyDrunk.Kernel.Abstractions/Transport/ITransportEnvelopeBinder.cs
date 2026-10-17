using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Abstractions.Transport;

/// <summary>
/// Binds GridContext to a transport envelope (HTTP response, message, job) for propagation.
/// </summary>
/// <remarks>
/// Transport binders enable automatic context propagation across different transport mechanisms
/// without coupling the core kernel to specific transport libraries. Each transport type
/// (HTTP, gRPC, messaging, jobs) implements its own binder.
/// </remarks>
public interface ITransportEnvelopeBinder
{
    /// <summary>
    /// Gets the transport type this binder handles (e.g., "http", "grpc", "message", "job").
    /// </summary>
    string TransportType { get; }

    /// <summary>
    /// Binds GridContext to the transport envelope.
    /// </summary>
    /// <param name="envelope">The transport envelope to bind to.</param>
    /// <param name="context">The GridContext to bind.</param>
    void Bind(object envelope, IGridContext context);

    /// <summary>
    /// Determines if this binder can handle the given envelope type.
    /// </summary>
    /// <param name="envelope">The envelope to check.</param>
    /// <returns>True if this binder can handle the envelope; otherwise false.</returns>
    bool CanBind(object envelope);
}
