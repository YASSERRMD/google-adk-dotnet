using Google.Adk.Models;
using Google.Adk.Tools;

namespace Google.Adk.Agents;

/// <summary>
/// Single-turn LLM-backed agent that can be composed into workflows.
/// </summary>
public sealed class LlmAgent : IAgent
{
    private readonly IChatModelClient _modelClient;
    private readonly string _name;

    public LlmAgent(IChatModelClient modelClient, string name = "llm")
    {
        _modelClient = modelClient ?? throw new ArgumentNullException(nameof(modelClient));
        _name = name;
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var callback in context.Callbacks)
        {
            await callback.OnStepStartAsync(_name, context, cancellationToken).ConfigureAwait(false);
        }

        var response = await _modelClient.GenerateAsync(context.Messages, cancellationToken).ConfigureAwait(false);
        var messages = new List<AgentMessage>();

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            messages.Add(AgentMessage.Assistant(response.Content));
        }

        if (response.ToolCalls.Count > 0)
        {
            foreach (var call in response.ToolCalls)
            {
                if (!context.ToolRegistry.TryGetTool(call.Name, out var tool))
                {
                    throw new InvalidOperationException($"Tool '{call.Name}' is not registered.");
                }

                var callMessage = new AgentMessage(MessageRole.Assistant, $"Calling {call.Name}", call);
                messages.Add(callMessage);
                var toolResult = await tool.ExecuteAsync(call, cancellationToken).ConfigureAwait(false);
                messages.Add(new AgentMessage(MessageRole.Tool, toolResult.Content, call, toolResult));
            }
        }

        foreach (var callback in context.Callbacks)
        {
            var lastMessage = messages.LastOrDefault() ?? AgentMessage.Assistant(string.Empty);
            await callback.OnStepEndAsync(_name, lastMessage, cancellationToken).ConfigureAwait(false);
        }

        return new AgentResult(messages, Array.Empty<string>(), completed: response.ToolCalls.Count == 0);
    }
}
