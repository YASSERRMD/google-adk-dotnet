# Google ADK for .NET

A .NET implementation of Google's Attestation Development Kit (ADK) and emerging agent runtime patterns, inspired by the Go reference (`google/adk-go`).

The repository now includes:

- A typed `AdkEnvelope` model and verification helpers for attestation payloads.
- An agent framework with LLM-backed agents, workflow agents (sequential, parallel, loop), multi-agent routing, tool/plugin abstractions, and in-memory session memory.
- A lightweight runtime server that exposes HTTP endpoints to run agents, list agents/tools, and manage sessions with a default echo agent and tool-calling support.
- A simple CLI (`adk`) for validating envelopes from the command line.
- Unit tests covering attestation verification and the new agent orchestration building blocks.

## Projects

- `Google.Adk` — core library for parsing and verifying envelopes.
- `Google.Adk.Cli` — lightweight CLI for working with envelope files.
- `Google.Adk.Tests` — xUnit-based tests exercising the verifier and serializer.

## Usage

### Verifying an envelope in code

```csharp
using Google.Adk;

var options = new AdkVerifierOptions();
options.TrustedRoots.Add(CertificateLoader.LoadCertificateFile("root.pem"));

var envelope = AdkEnvelopeSerializer.ReadFromFile("envelope.json");
var verifier = new AdkVerifier(options);
var result = verifier.Verify(envelope);

if (result.IsValid)
{
    Console.WriteLine("Attestation verified");
}
```

### Verifying from the CLI

```bash
adk verify path/to/envelope.json --root path/to/root.pem --hash SHA256
```

## Agent runtime quickstart

Run the runtime server locally:

```bash
dotnet run --project src/Google.Adk.Runtime/Google.Adk.Runtime.csproj
```

Invoke the default echo agent:

```bash
curl -X POST "http://localhost:5000/v1/agents/echo:run" \
  -H "Content-Type: application/json" \
  -d '{"prompt":"hello agent"}'
```

The response includes the session identifier and accumulated messages so you can keep sending additional prompts while preserving memory.

List sessions or fetch a conversation transcript:

```bash
curl http://localhost:5000/v1/sessions
curl http://localhost:5000/v1/sessions/<sessionId>
```

## Development

This repository includes a standard .NET solution (`Google.Adk.sln`). Run `dotnet restore` followed by `dotnet test` to build and validate the code once the .NET SDK is available in your environment.
