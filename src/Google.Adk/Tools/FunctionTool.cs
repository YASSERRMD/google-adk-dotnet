using System.Text.Json;

namespace Google.Adk.Tools;

/// <summary>
/// Wraps a C# delegate as a tool invocation target.
/// </summary>
public sealed class FunctionTool : ITool
{
    private readonly Func<ToolInvocation, CancellationToken, Task<ToolResult>> _handler;

    public FunctionTool(
        string name,
        string description,
        Func<ToolInvocation, CancellationToken, Task<ToolResult>> handler,
        JsonDocument? inputSchema = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        InputSchema = inputSchema;
    }

    public string Name { get; }

    public string Description { get; }

    public JsonDocument? InputSchema { get; }

    public Task<ToolResult> ExecuteAsync(ToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        return _handler(invocation, cancellationToken);
    }
}
