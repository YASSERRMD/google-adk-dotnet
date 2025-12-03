using System.Security.Cryptography.X509Certificates;

namespace Google.Adk;

/// <summary>
/// Represents a signed attestation package produced by an ADK signer.
/// </summary>
public sealed class AdkEnvelope
{
    public AdkEnvelope(
        byte[] payload,
        byte[] signature,
        IReadOnlyList<X509Certificate2> certificates,
        string? algorithm)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Signature = signature ?? throw new ArgumentNullException(nameof(signature));
        Certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        Algorithm = algorithm;

        if (Payload.Length == 0)
        {
            throw new ArgumentException("Envelope payload cannot be empty.", nameof(payload));
        }

        if (Signature.Length == 0)
        {
            throw new ArgumentException("Envelope signature cannot be empty.", nameof(signature));
        }

        if (Certificates.Count == 0)
        {
            throw new ArgumentException("Envelope must contain at least one certificate.", nameof(certificates));
        }
    }

    public byte[] Payload { get; }

    public byte[] Signature { get; }

    public IReadOnlyList<X509Certificate2> Certificates { get; }

    public string? Algorithm { get; }

    /// <summary>
    /// Leaf certificate used to produce the signature.
    /// </summary>
    public X509Certificate2 LeafCertificate => Certificates[0];
}
