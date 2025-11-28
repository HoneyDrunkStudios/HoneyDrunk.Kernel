using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Transport;

namespace HoneyDrunk.Kernel.Transport;

/// <summary>
/// Binds GridContext to message properties for propagation through messaging systems.
/// </summary>
/// <remarks>
/// This binder assumes message envelopes are dictionaries (common pattern for most
/// messaging systems like RabbitMQ, Azure Service Bus, AWS SQS). For strongly-typed
/// message envelopes, implement a custom binder.
/// </remarks>
public sealed class MessagePropertiesBinder : ITransportEnvelopeBinder
{
    /// <inheritdoc />
    public string TransportType => "message";

    /// <inheritdoc />
    public bool CanBind(object envelope) => envelope is IDictionary<string, object>;

    /// <inheritdoc />
    public void Bind(object envelope, IGridContext context)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        if (envelope is not IDictionary<string, object> properties)
        {
            throw new ArgumentException($"Expected IDictionary<string, object> but got {envelope.GetType().Name}", nameof(envelope));
        }

        // Bind Grid context to message properties
        properties[GridHeaderNames.CorrelationId] = context.CorrelationId;
        properties[GridHeaderNames.NodeId] = context.NodeId;

        if (context.CausationId is not null)
        {
            properties[GridHeaderNames.CausationId] = context.CausationId;
        }

        properties[GridHeaderNames.StudioId] = context.StudioId;
        properties[GridHeaderNames.Environment] = context.Environment;

        // Bind baggage as prefixed properties
        foreach (var (key, value) in context.Baggage)
        {
            properties[$"{GridHeaderNames.BaggagePrefix}{key}"] = value;
        }
    }
}
