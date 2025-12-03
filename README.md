# Google ADK for .NET

A .NET implementation of Google's Attestation Development Kit (ADK) concepts, inspired by the Go reference (`google/adk-go`).

The library provides:

- A typed `AdkEnvelope` model for representing signed payloads and certificate chains.
- JSON serialization helpers for envelopes.
- Signature and certificate chain verification with configurable trust roots.
- A simple CLI (`adk`) for validating envelopes from the command line.
- Unit tests demonstrating envelope generation, serialization, and verification.

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

## Development

This repository includes a standard .NET solution (`Google.Adk.sln`). Run `dotnet restore` followed by `dotnet test` to build and validate the code once the .NET SDK is available in your environment.
