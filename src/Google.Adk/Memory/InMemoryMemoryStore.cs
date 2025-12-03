using Google.Adk.Agents;

namespace Google.Adk.Memory;

/// <summary>
/// Simple thread-safe memory store backed by in-memory collections.
/// </summary>
public sealed class InMemoryMemoryStore : IMemoryStore
{
    private readonly Dictionary<string, List<AgentMessage>> _sessions = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<AgentMessage>> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessions.TryGetValue(sessionId, out var messages))
            {
                return messages.ToList();
            }

            return Array.Empty<AgentMessage>();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AppendAsync(string sessionId, IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var existing))
            {
                existing = new List<AgentMessage>();
                _sessions[sessionId] = existing;
            }

            existing.AddRange(messages);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<string>> ListSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _sessions.Keys.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }
}
