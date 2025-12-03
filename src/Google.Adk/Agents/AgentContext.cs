using Google.Adk.Callbacks;
using Google.Adk.Memory;
using Google.Adk.Tools;

namespace Google.Adk.Agents;

/// <summary>
/// Provides execution-time dependencies to agents.
/// </summary>
public sealed class AgentContext
{
    private readonly List<AgentMessage> _messages = new();
    private readonly List<IAgentCallback> _callbacks = new();

    public AgentContext(
        AgentConfig config,
        IToolRegistry toolRegistry,
        IMemoryStore memoryStore,
        string sessionId)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        ToolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        MemoryStore = memoryStore ?? throw new ArgumentNullException(nameof(memoryStore));
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

        if (!string.IsNullOrWhiteSpace(config.SystemPrompt))
        {
            _messages.Add(AgentMessage.System(config.SystemPrompt!));
        }
    }

    public AgentConfig Config { get; }

    public string SessionId { get; }

    public IToolRegistry ToolRegistry { get; }

    public IMemoryStore MemoryStore { get; }

    public IReadOnlyList<AgentMessage> Messages => _messages;

    public IReadOnlyList<IAgentCallback> Callbacks => _callbacks;

    public void AddMessage(AgentMessage message) => _messages.Add(message);

    public void AddCallback(IAgentCallback callback) => _callbacks.Add(callback);

    public AgentContext Clone()
    {
        var clone = new AgentContext(Config, ToolRegistry, MemoryStore, SessionId);
        foreach (var message in _messages)
        {
            clone._messages.Add(message);
        }

        foreach (var callback in _callbacks)
        {
            clone._callbacks.Add(callback);
        }

        return clone;
    }
}
