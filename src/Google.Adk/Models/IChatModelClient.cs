using Google.Adk.Agents;
using Google.Adk.Tools;

namespace Google.Adk.Models;

/// <summary>
/// Minimal abstraction over a chat-capable model.
/// </summary>
public interface IChatModelClient
{
    Task<ChatModelResponse> GenerateAsync(IEnumerable<AgentMessage> history, CancellationToken cancellationToken = default);
}

/// <summary>
/// Structured response from a chat model invocation.
/// </summary>
/// <param name="Content">Assistant text response.</param>
/// <param name="ToolCalls">Optional tool call requests emitted by the model.</param>
public sealed record ChatModelResponse(string Content, IReadOnlyList<ToolInvocation> ToolCalls)
{
    public static ChatModelResponse Text(string content) => new(content, Array.Empty<ToolInvocation>());
}

/// <summary>
/// Debug-friendly model that simply echoes the last user message.
/// </summary>
public sealed class EchoModelClient : IChatModelClient
{
    public Task<ChatModelResponse> GenerateAsync(IEnumerable<AgentMessage> history, CancellationToken cancellationToken = default)
    {
        var content = history.LastOrDefault(m => m.Role == MessageRole.User)?.Content ?? string.Empty;
        return Task.FromResult(ChatModelResponse.Text($"echo: {content}"));
    }
}
