using Google.Adk.Agents;
using Google.Adk.Memory;
using Google.Adk.Tools;

namespace Google.Adk.Runtime;

/// <summary>
/// Coordinates agent execution for the HTTP runtime server.
/// </summary>
public sealed class AgentRuntimeHost
{
    private readonly Dictionary<string, Func<IAgent>> _agentFactories = new(StringComparer.OrdinalIgnoreCase);

    public AgentRuntimeHost(IToolRegistry toolRegistry, IMemoryStore memoryStore)
    {
        ToolRegistry = toolRegistry;
        MemoryStore = memoryStore;
    }

    public IToolRegistry ToolRegistry { get; }

    public IMemoryStore MemoryStore { get; }

    public IReadOnlyCollection<string> RegisteredAgents => _agentFactories.Keys;

    public void RegisterAgent(string id, Func<IAgent> factory)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _agentFactories[id] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<AgentResult> RunAsync(string id, string prompt, string sessionId, CancellationToken cancellationToken = default)
    {
        if (!_agentFactories.TryGetValue(id, out var factory))
        {
            throw new InvalidOperationException($"Agent '{id}' is not registered.");
        }

        var history = await MemoryStore.GetSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var config = new AgentConfig { SystemPrompt = "Google ADK .NET runtime" };
        var context = new AgentContext(config, ToolRegistry, MemoryStore, sessionId);
        var accumulated = new List<AgentMessage>(history);
        foreach (var message in history)
        {
            context.AddMessage(message);
        }

        var userMessage = AgentMessage.User(prompt);
        context.AddMessage(userMessage);
        accumulated.Add(userMessage);

        var agent = factory();
        var result = await agent.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        accumulated.AddRange(result.Messages);

        var newMessages = new List<AgentMessage> { userMessage };
        newMessages.AddRange(result.Messages);

        await MemoryStore.AppendAsync(sessionId, newMessages, cancellationToken).ConfigureAwait(false);
        return new AgentResult(accumulated, result.Artifacts, result.Completed);
    }
}
