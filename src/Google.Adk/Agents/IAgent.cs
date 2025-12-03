namespace Google.Adk.Agents;

/// <summary>
/// Contract implemented by all agents.
/// </summary>
public interface IAgent
{
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
}
