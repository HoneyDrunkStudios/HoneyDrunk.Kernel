namespace HoneyDrunk.Kernel.Abstractions.Agents;

/// <summary>
/// Defines what portions of Grid context an agent can access.
/// </summary>
/// <remarks>
/// Context scope controls what information is visible to agents for privacy and security.
/// Agents should only see the minimum context needed to perform their tasks.
/// </remarks>
public enum AgentContextScope
{
    /// <summary>
    /// Agent can see no context (fully isolated).
    /// </summary>
    None = 0,

    /// <summary>
    /// Agent can see basic correlation/causation IDs only.
    /// </summary>
    CorrelationOnly = 1,

    /// <summary>
    /// Agent can see Node identity and correlation data.
    /// </summary>
    NodeAndCorrelation = 2,

    /// <summary>
    /// Agent can see Studio, environment, and Node data.
    /// </summary>
    StudioAndNode = 3,

    /// <summary>
    /// Agent can see all non-sensitive Grid context (excludes baggage with secrets).
    /// </summary>
    Standard = 4,

    /// <summary>
    /// Agent has full access to all Grid context including baggage.
    /// </summary>
    Full = 5,
}
