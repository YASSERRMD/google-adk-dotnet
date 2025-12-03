using Google.Adk.Agents;

namespace Google.Adk.Callbacks;

/// <summary>
/// Extensibility hooks invoked during agent execution.
/// </summary>
public interface IAgentCallback
{
    Task OnStepStartAsync(string agentName, AgentContext context, CancellationToken cancellationToken = default);

    Task OnStepEndAsync(string agentName, AgentMessage output, CancellationToken cancellationToken = default);

    Task OnErrorAsync(string agentName, Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>
/// Simple console logger callback.
/// </summary>
public sealed class LoggingAgentCallback : IAgentCallback
{
    public Task OnStepStartAsync(string agentName, AgentContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{agentName}] starting step with {context.Messages.Count} messages.");
        return Task.CompletedTask;
    }

    public Task OnStepEndAsync(string agentName, AgentMessage output, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{agentName}] -> {output.Content}");
        return Task.CompletedTask;
    }
}
