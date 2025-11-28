using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Transport;

namespace HoneyDrunk.Kernel.Transport;

/// <summary>
/// Binds GridContext to job metadata for propagation through background job systems.
/// </summary>
/// <remarks>
/// This binder assumes job metadata is stored as a dictionary (common for systems
/// like Hangfire, Quartz, Azure Functions). For strongly-typed job contexts,
/// implement a custom binder.
/// </remarks>
public sealed class JobMetadataBinder : ITransportEnvelopeBinder
{
    /// <inheritdoc />
    public string TransportType => "job";

    /// <inheritdoc />
    public bool CanBind(object envelope) => envelope is IDictionary<string, string>;

    /// <inheritdoc />
    public void Bind(object envelope, IGridContext context)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        if (envelope is not IDictionary<string, string> metadata)
        {
            throw new ArgumentException($"Expected IDictionary<string, string> but got {envelope.GetType().Name}", nameof(envelope));
        }

        // Bind Grid context to job metadata
        metadata[GridHeaderNames.CorrelationId] = context.CorrelationId;
        metadata[GridHeaderNames.NodeId] = context.NodeId;

        if (context.CausationId is not null)
        {
            metadata[GridHeaderNames.CausationId] = context.CausationId;
        }

        metadata[GridHeaderNames.StudioId] = context.StudioId;
        metadata[GridHeaderNames.Environment] = context.Environment;
        metadata["CreatedAtUtc"] = context.CreatedAtUtc.ToString("O");

        // Bind baggage as prefixed metadata
        foreach (var (key, value) in context.Baggage)
        {
            metadata[$"{GridHeaderNames.BaggagePrefix}{key}"] = value;
        }
    }
}
