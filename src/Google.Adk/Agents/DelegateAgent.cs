namespace Google.Adk.Agents;

/// <summary>
/// Wraps a delegate as an agent for quick experimentation.
/// </summary>
public sealed class DelegateAgent : IAgent
{
    private readonly Func<AgentContext, CancellationToken, Task<AgentResult>> _executor;

    public DelegateAgent(Func<AgentContext, CancellationToken, Task<AgentResult>> executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _executor(context, cancellationToken);
    }
}
