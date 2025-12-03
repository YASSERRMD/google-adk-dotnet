using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Google.Adk;

/// <summary>
/// Represents the output of an envelope verification attempt.
/// </summary>
public sealed class AdkVerificationResult
{
    public AdkVerificationResult(
        bool chainValid,
        bool signatureValid,
        IReadOnlyList<string> errors,
        byte[] payload,
        JsonDocument? payloadJson,
        X509Certificate2 leafCertificate)
    {
        IsChainValid = chainValid;
        IsSignatureValid = signatureValid;
        Errors = errors;
        Payload = payload;
        PayloadJson = payloadJson;
        LeafCertificate = leafCertificate;
    }

    public bool IsChainValid { get; }

    public bool IsSignatureValid { get; }

    public bool IsValid => IsChainValid && IsSignatureValid && Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }

    public byte[] Payload { get; }

    public JsonDocument? PayloadJson { get; }

    public X509Certificate2 LeafCertificate { get; }
}
