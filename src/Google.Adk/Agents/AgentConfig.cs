namespace Google.Adk.Agents;

/// <summary>
/// Configuration controlling how an agent executes.
/// </summary>
public sealed class AgentConfig
{
    public string? SystemPrompt { get; init; }

    public int MaxSteps { get; init; } = 12;

    public int MaxIterations { get; init; } = 12;

    public TimeSpan? ExecutionTimeout { get; init; }

    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}
