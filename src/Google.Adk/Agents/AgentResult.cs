namespace Google.Adk.Agents;

/// <summary>
/// Result of an agent execution.
/// </summary>
public sealed class AgentResult
{
    public AgentResult(
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<string> artifacts,
        bool completed)
    {
        Messages = messages;
        Artifacts = artifacts;
        Completed = completed;
    }

    public IReadOnlyList<AgentMessage> Messages { get; }

    public IReadOnlyList<string> Artifacts { get; }

    public bool Completed { get; }

    public static AgentResult SingleMessage(AgentMessage message) =>
        new(new List<AgentMessage> { message }, Array.Empty<string>(), completed: true);
}
