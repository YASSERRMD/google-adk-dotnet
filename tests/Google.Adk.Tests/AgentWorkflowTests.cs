using System.Collections.Generic;
using Google.Adk.Agents;
using Google.Adk.Memory;
using Google.Adk.Models;
using Google.Adk.Runtime;
using Google.Adk.Tools;
using Xunit;

namespace Google.Adk.Tests;

public class AgentWorkflowTests
{
    [Fact]
    public async Task LlmAgent_UsesModelClient()
    {
        var model = new EchoModelClient();
        var agent = new LlmAgent(model, "llm");
        var context = new AgentContext(new AgentConfig(), new InMemoryToolRegistry(), new InMemoryMemoryStore(), "session-1");
        context.AddMessage(AgentMessage.User("hello"));

        var result = await agent.ExecuteAsync(context);

        Assert.Single(result.Messages);
        Assert.Contains("hello", result.Messages[0].Content);
    }

    [Fact]
    public async Task LlmAgent_InvokesRegisteredTools()
    {
        var toolInvoked = false;
        var model = new StubToolCallingModel();
        var registry = new InMemoryToolRegistry();
        registry.Register(new FunctionTool("echoTool", "echo", (invocation, _) =>
        {
            toolInvoked = true;
            return Task.FromResult(new ToolResult(true, $"tool:{invocation.Arguments["input"]}"));
        }));

        var agent = new LlmAgent(model, "llm");
        var context = new AgentContext(new AgentConfig(), registry, new InMemoryMemoryStore(), "session-tools");
        context.AddMessage(AgentMessage.User("hi"));

        var result = await agent.ExecuteAsync(context);

        Assert.True(toolInvoked);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(MessageRole.Assistant, result.Messages[0].Role);
        Assert.Equal(MessageRole.Tool, result.Messages[1].Role);
        Assert.False(result.Completed);
    }

    [Fact]
    public async Task SequentialAgent_ComposesMessages()
    {
        var first = new DelegateAgent((ctx, _) => Task.FromResult(AgentResult.SingleMessage(AgentMessage.Assistant("first"))))
        ;
        var second = new DelegateAgent((ctx, _) => Task.FromResult(AgentResult.SingleMessage(AgentMessage.Assistant("second"))));
        var agent = new SequentialAgent(first, second);
        var context = new AgentContext(new AgentConfig(), new InMemoryToolRegistry(), new InMemoryMemoryStore(), "session-2");

        var result = await agent.ExecuteAsync(context);

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("first", result.Messages[0].Content);
        Assert.Equal("second", result.Messages[1].Content);
    }

    [Fact]
    public async Task ParallelAgent_AggregatesOutputs()
    {
        var a = new DelegateAgent((ctx, _) => Task.FromResult(AgentResult.SingleMessage(AgentMessage.Assistant("A"))));
        var b = new DelegateAgent((ctx, _) => Task.FromResult(AgentResult.SingleMessage(AgentMessage.Assistant("B"))));
        var agent = new ParallelAgent(a, b);
        var context = new AgentContext(new AgentConfig(), new InMemoryToolRegistry(), new InMemoryMemoryStore(), "session-3");

        var result = await agent.ExecuteAsync(context);

        Assert.Equal(2, result.Messages.Count);
        Assert.Contains(result.Messages, m => m.Content == "A");
        Assert.Contains(result.Messages, m => m.Content == "B");
    }

    [Fact]
    public async Task LoopAgent_StopsWhenConditionMet()
    {
        var counter = 0;
        var inner = new DelegateAgent((ctx, _) =>
        {
            counter++;
            return Task.FromResult(AgentResult.SingleMessage(AgentMessage.Assistant($"tick {counter}")));
        });

        var loop = new LoopAgent(inner, messages => messages.Any(m => m.Content.Contains("tick 3")));
        var context = new AgentContext(new AgentConfig { MaxIterations = 10 }, new InMemoryToolRegistry(), new InMemoryMemoryStore(), "session-4");

        var result = await loop.ExecuteAsync(context);

        Assert.True(counter >= 3);
        Assert.Contains(result.Messages, m => m.Content.Contains("tick 3"));
    }

    [Fact]
    public async Task AgentRuntimeHost_PersistsSessions()
    {
        var registry = new InMemoryToolRegistry();
        var memory = new InMemoryMemoryStore();
        var host = new AgentRuntimeHost(registry, memory);

        host.RegisterAgent("echo", () => new DelegateAgent((ctx, _) => Task.FromResult(AgentResult.SingleMessage(AgentMessage.Assistant(ctx.Messages.Last().Content + "!")))));

        var first = await host.RunAsync("echo", "hi", "session-5");
        var second = await host.RunAsync("echo", "again", "session-5");

        Assert.Equal(2, first.Messages.Count);
        Assert.Equal(MessageRole.User, first.Messages[0].Role);
        Assert.Equal(MessageRole.Assistant, first.Messages[1].Role);

        Assert.Equal(4, second.Messages.Count);
        Assert.Equal(MessageRole.User, second.Messages[2].Role);
        Assert.Equal("again", second.Messages[2].Content);
        var history = await memory.GetSessionAsync("session-5");
        Assert.Equal(4, history.Count);
    }
}

file static class ModelStubs
{
    public sealed class StubToolCallingModel : IChatModelClient
    {
        public Task<ChatModelResponse> GenerateAsync(IEnumerable<AgentMessage> history, CancellationToken cancellationToken = default)
        {
            var call = new ToolInvocation("echoTool", new Dictionary<string, object?> { { "input", "hi" } });
            return Task.FromResult(new ChatModelResponse(string.Empty, new[] { call }));
        }
    }
}
