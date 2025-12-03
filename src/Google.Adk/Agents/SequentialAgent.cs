namespace Google.Adk.Agents;

/// <summary>
/// Executes a set of child agents in order, threading the conversation.
/// </summary>
public sealed class SequentialAgent : IAgent
{
    private readonly IReadOnlyList<IAgent> _agents;

    public SequentialAgent(params IAgent[] agents)
    {
        if (agents.Length == 0)
        {
            throw new ArgumentException("SequentialAgent requires at least one child agent.", nameof(agents));
        }

        _agents = agents;
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var messages = new List<AgentMessage>(context.Messages);

        foreach (var agent in _agents)
        {
            var childContext = context.Clone();
            foreach (var message in messages)
            {
                childContext.AddMessage(message);
            }

            var result = await agent.ExecuteAsync(childContext, cancellationToken).ConfigureAwait(false);
            messages.AddRange(result.Messages);
        }

        return new AgentResult(messages, Array.Empty<string>(), completed: true);
    }
}
