namespace Google.Adk.Agents;

/// <summary>
/// Executes multiple agents concurrently and aggregates their responses.
/// </summary>
public sealed class ParallelAgent : IAgent
{
    private readonly IReadOnlyList<IAgent> _agents;

    public ParallelAgent(params IAgent[] agents)
    {
        if (agents.Length == 0)
        {
            throw new ArgumentException("ParallelAgent requires at least one child agent.", nameof(agents));
        }

        _agents = agents;
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var tasks = _agents.Select(agent => agent.ExecuteAsync(context.Clone(), cancellationToken)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var messages = results.SelectMany(r => r.Messages).ToList();
        return new AgentResult(messages, Array.Empty<string>(), completed: true);
    }
}
