using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Transport;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Transport;

/// <summary>
/// Binds GridContext to HTTP response headers for propagation.
/// </summary>
public sealed class HttpResponseBinder : ITransportEnvelopeBinder
{
    /// <inheritdoc />
    public string TransportType => "http";

    /// <inheritdoc />
    public bool CanBind(object envelope) => envelope is HttpResponse;

    /// <inheritdoc />
    public void Bind(object envelope, IGridContext context)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        if (envelope is not HttpResponse response)
        {
            throw new ArgumentException($"Expected HttpResponse but got {envelope.GetType().Name}", nameof(envelope));
        }

        // Bind Grid context to response headers for client tracing
        response.Headers[GridHeaderNames.CorrelationId] = context.CorrelationId;
        response.Headers[GridHeaderNames.NodeId] = context.NodeId;

        if (context.CausationId is not null)
        {
            response.Headers[GridHeaderNames.CausationId] = context.CausationId;
        }

        // Bind baggage (prefix to avoid conflicts)
        foreach (var (key, value) in context.Baggage)
        {
            response.Headers[$"{GridHeaderNames.BaggagePrefix}{key}"] = value;
        }
    }
}
