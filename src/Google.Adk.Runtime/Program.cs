using Google.Adk.Agents;
using Google.Adk.Memory;
using Google.Adk.Models;
using Google.Adk.Runtime;
using Google.Adk.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IToolRegistry, InMemoryToolRegistry>();
builder.Services.AddSingleton<IMemoryStore, InMemoryMemoryStore>();
builder.Services.AddSingleton<AgentRuntimeHost>();
builder.Services.AddSingleton<IChatModelClient, EchoModelClient>();

var app = builder.Build();
var runtime = app.Services.GetRequiredService<AgentRuntimeHost>();
var model = app.Services.GetRequiredService<IChatModelClient>();

// Register a default echo agent for quick experimentation.
runtime.RegisterAgent("echo", () => new LlmAgent(model, "echo"));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/v1/agents", (AgentRuntimeHost host) => Results.Ok(host.RegisteredAgents));

app.MapGet("/v1/tools", (IToolRegistry registry) => Results.Ok(registry.Tools.Select(t => new
{
    t.Name,
    t.Description,
    Schema = t.InputSchema?.RootElement.GetRawText()
})));

app.MapGet("/v1/sessions", async (IMemoryStore memory, CancellationToken ct) =>
{
    var sessions = await memory.ListSessionIdsAsync(ct).ConfigureAwait(false);
    return Results.Ok(sessions);
});

app.MapGet("/v1/sessions/{id}", async (string id, IMemoryStore memory, CancellationToken ct) =>
{
    var history = await memory.GetSessionAsync(id, ct).ConfigureAwait(false);
    return Results.Ok(history);
});

app.MapPost("/v1/sessions", () => Results.Ok(new { SessionId = Guid.NewGuid().ToString("N") }));

app.MapPost("/v1/agents/{id}:run", async (string id, RunAgentRequest request, AgentRuntimeHost host, CancellationToken ct) =>
{
    var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId!;
    var result = await host.RunAsync(id, request.Prompt, sessionId, ct).ConfigureAwait(false);
    return Results.Ok(new RunAgentResponse(sessionId, result.Messages));
});

app.Run();

internal sealed record RunAgentRequest(string Prompt, string? SessionId);
internal sealed record RunAgentResponse(string SessionId, IReadOnlyList<AgentMessage> Messages);
