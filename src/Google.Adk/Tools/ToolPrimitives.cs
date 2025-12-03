using System.Text.Json;

namespace Google.Adk.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonDocument? InputSchema { get; }
    Task<ToolResult> ExecuteAsync(ToolInvocation invocation, CancellationToken cancellationToken = default);
}

public sealed class ToolInvocation
{
    public ToolInvocation(string name, IReadOnlyDictionary<string, object?> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, object?> Arguments { get; }
}

public sealed class ToolResult
{
    public ToolResult(bool success, string content, JsonDocument? metadata = null)
    {
        Success = success;
        Content = content;
        Metadata = metadata;
    }

    public bool Success { get; }
    public string Content { get; }
    public JsonDocument? Metadata { get; }
}
