using System.Text.Json;
using Google.Adk.Tools;

namespace Google.Adk.Agents;

/// <summary>
/// Represents a single message exchanged between an agent, user, or tool.
/// </summary>
public sealed record AgentMessage(
    MessageRole Role,
    string Content,
    ToolInvocation? ToolCall = null,
    ToolResult? ToolResult = null,
    JsonDocument? AdditionalData = null)
{
    public static AgentMessage User(string content) => new(MessageRole.User, content);

    public static AgentMessage Assistant(string content) => new(MessageRole.Assistant, content);

    public static AgentMessage System(string content) => new(MessageRole.System, content);
}

/// <summary>
/// Roles supported by the messaging system.
/// </summary>
public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool
}
