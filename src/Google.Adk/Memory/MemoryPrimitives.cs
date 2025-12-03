using Google.Adk.Agents;

namespace Google.Adk.Memory;

public interface IMemoryStore
{
    Task<IReadOnlyList<AgentMessage>> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task AppendAsync(string sessionId, IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListSessionIdsAsync(CancellationToken cancellationToken = default);
}

public sealed class Session
{
    public Session(string id, IReadOnlyList<AgentMessage> messages)
    {
        Id = id;
        Messages = messages;
    }

    public string Id { get; }

    public IReadOnlyList<AgentMessage> Messages { get; }
}
