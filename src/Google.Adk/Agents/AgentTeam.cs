namespace Google.Adk.Agents;

/// <summary>
/// Basic multi-agent system with routing logic.
/// </summary>
public sealed class AgentTeam : IAgent
{
    private readonly IReadOnlyDictionary<string, IAgent> _agents;
    private readonly Func<IReadOnlyList<AgentMessage>, string> _router;

    public AgentTeam(IDictionary<string, IAgent> agents, Func<IReadOnlyList<AgentMessage>, string> router)
    {
        if (agents.Count == 0)
        {
            throw new ArgumentException("At least one agent is required.", nameof(agents));
        }

        _agents = new Dictionary<string, IAgent>(agents);
        _router = router ?? throw new ArgumentNullException(nameof(router));
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var messages = new List<AgentMessage>(context.Messages);

        for (var step = 0; step < context.Config.MaxSteps; step++)
        {
            var target = _router(messages);
            if (!_agents.TryGetValue(target, out var agent))
            {
                throw new InvalidOperationException($"Router selected unknown agent '{target}'.");
            }

            var childContext = context.Clone();
            foreach (var message in messages)
            {
                childContext.AddMessage(message);
            }

            var result = await agent.ExecuteAsync(childContext, cancellationToken).ConfigureAwait(false);
            messages.AddRange(result.Messages);

            if (result.Completed)
            {
                break;
            }
        }

        return new AgentResult(messages, Array.Empty<string>(), completed: true);
    }
}
