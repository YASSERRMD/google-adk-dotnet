namespace Google.Adk.Agents;

/// <summary>
/// Repeatedly executes a child agent until a stop condition is met.
/// </summary>
public sealed class LoopAgent : IAgent
{
    private readonly IAgent _inner;
    private readonly Func<IReadOnlyList<AgentMessage>, bool> _shouldStop;

    public LoopAgent(IAgent inner, Func<IReadOnlyList<AgentMessage>, bool> shouldStop)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _shouldStop = shouldStop ?? throw new ArgumentNullException(nameof(shouldStop));
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var messages = new List<AgentMessage>(context.Messages);

        for (var i = 0; i < context.Config.MaxIterations; i++)
        {
            var innerContext = context.Clone();
            foreach (var message in messages)
            {
                innerContext.AddMessage(message);
            }

            var result = await _inner.ExecuteAsync(innerContext, cancellationToken).ConfigureAwait(false);
            messages.AddRange(result.Messages);

            if (_shouldStop(messages))
            {
                break;
            }
        }

        return new AgentResult(messages, Array.Empty<string>(), completed: true);
    }
}
