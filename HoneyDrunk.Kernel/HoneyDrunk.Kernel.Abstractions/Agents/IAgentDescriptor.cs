namespace HoneyDrunk.Kernel.Abstractions.Agents;

/// <summary>
/// Describes an agent's identity, capabilities, and access permissions within the Grid.
/// </summary>
/// <remarks>
/// Agent descriptors define what an agent is and what it's allowed to do.
/// This enables fine-grained authorization and capability-based security.
/// Agents can be LLM-based assistants, automation scripts, or service accounts.
/// </remarks>
public interface IAgentDescriptor
{
    /// <summary>
    /// Gets the unique identifier for this agent.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the human-readable name of this agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the agent type/category.
    /// Examples: "llm-assistant", "automation-bot", "service-account".
    /// </summary>
    string AgentType { get; }

    /// <summary>
    /// Gets the version of this agent.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the capabilities this agent possesses.
    /// Capabilities define what actions the agent can perform.
    /// </summary>
    IReadOnlyList<IAgentCapability> Capabilities { get; }

    /// <summary>
    /// Gets the scope of Grid context this agent can access.
    /// Defines what data from GridContext is visible to the agent.
    /// </summary>
    AgentContextScope ContextScope { get; }

    /// <summary>
    /// Gets agent-specific metadata and configuration.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Determines if this agent has a specific capability.
    /// </summary>
    /// <param name="capabilityName">The capability name to check.</param>
    /// <returns>True if the agent has the capability; otherwise false.</returns>
    bool HasCapability(string capabilityName);
}
