namespace Google.Adk.Tools;

public interface IToolRegistry
{
    IEnumerable<ITool> Tools { get; }
    void Register(ITool tool);
    bool TryGetTool(string name, out ITool tool);
}

/// <summary>
/// Thread-safe in-memory registry for tool discovery.
/// </summary>
public sealed class InMemoryToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ITool> Tools => _tools.Values;

    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools[tool.Name] = tool;
    }

    public bool TryGetTool(string name, out ITool tool) => _tools.TryGetValue(name, out tool!);
}
