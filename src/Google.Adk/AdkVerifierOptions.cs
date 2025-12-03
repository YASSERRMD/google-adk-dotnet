using System.Security.Cryptography.X509Certificates;

namespace Google.Adk;

/// <summary>
/// Options controlling how attestation envelopes are validated.
/// </summary>
public sealed class AdkVerifierOptions
{
    public List<X509Certificate2> TrustedRoots { get; } = new();

    /// <summary>
    /// Optional timestamp to evaluate certificate validity and expiration.
    /// </summary>
    public DateTimeOffset EvaluationTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Hash algorithm used to validate signatures when none is specified by the envelope.
    /// </summary>
    public HashAlgorithmName DefaultHashAlgorithm { get; init; } = HashAlgorithmName.SHA256;
}
