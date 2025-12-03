# Google ADK for .NET

A .NET implementation of Google's **Agent Development Kit (ADK)** with an experimental agent runtime adapted from the Go reference (`google/adk-go`).

> This project ports and extends the Google Agent Development Kit from the official Go implementation at https://github.com/google/adk-go.

## What's included

- A typed `AdkEnvelope` model and verification helpers for attestation payloads.
- An agent framework with LLM-backed agents, workflow agents (sequential, parallel, loop), multi-agent routing, tool/plugin abstractions, and in-memory session memory.
- A lightweight runtime server with HTTP endpoints to run agents, list agents/tools, and manage sessions (ships with a default echo agent and tool-calling support).
- A CLI (`adk`) for validating envelopes from the command line.
- xUnit tests covering attestation verification and the agent orchestration building blocks.

## Prerequisites

- .NET 8 SDK
- A PEM-encoded trusted root certificate to validate envelopes

## Project layout

- `Google.Adk` — Core library for parsing and verifying envelopes.
- `Google.Adk.Cli` — Lightweight CLI for working with envelope files.
- `Google.Adk.Runtime` — Minimal runtime server that hosts agents and exposes HTTP endpoints.
- `Google.Adk.Tests` — xUnit-based tests exercising the verifier and serializer.

## Usage

### Verify an envelope in code

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

### Verify from the CLI

Run the CLI directly from the repository:

```bash
dotnet run --project src/Google.Adk.Cli/Google.Adk.Cli.csproj -- \
  verify path/to/envelope.json --root path/to/root.pem --hash SHA256
```

Optional switches:

- `--time <ISO-8601>` to validate at a specific instant.
- `--hash <SHA256|SHA384|SHA512>` to select a hash algorithm.
- Repeat `--root` to trust multiple certificates.

## Agent runtime quickstart

Run the runtime server locally (defaults to `http://localhost:5000`):

```bash
dotnet run --project src/Google.Adk.Runtime/Google.Adk.Runtime.csproj
```

Invoke the default echo agent:

```bash
curl -X POST "http://localhost:5000/v1/agents/echo:run" \
  -H "Content-Type: application/json" \
  -d '{"prompt":"hello agent"}'
```

The response includes a `sessionId` plus the accumulated `messages` so you can continue the conversation while preserving memory.

List sessions or fetch a conversation transcript:

```bash
curl http://localhost:5000/v1/sessions
curl http://localhost:5000/v1/sessions/<sessionId>
```

List the registered agents and tools:

```bash
curl http://localhost:5000/v1/agents
curl http://localhost:5000/v1/tools
```

## Development

Restore dependencies and run the test suite:

```bash
dotnet restore
dotnet test
```

Use `dotnet build` if you need a compiled output. The solution file is `Google.Adk.sln`.
