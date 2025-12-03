using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Google.Adk;

/// <summary>
/// Validates ADK envelopes by inspecting signatures and certificate chains.
/// </summary>
public sealed class AdkVerifier
{
    private readonly AdkVerifierOptions _options;

    public AdkVerifier(AdkVerifierOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public AdkVerificationResult Verify(AdkEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var errors = new List<string>();
        var chainValid = TryValidateChain(envelope, errors);
        var signatureValid = TryValidateSignature(envelope, errors, out var hashAlgorithm);
        var payloadJson = TryParseJson(envelope.Payload, errors);

        if (!chainValid)
        {
            errors.Add("Certificate chain validation failed.");
        }

        if (!signatureValid)
        {
            errors.Add($"Payload signature validation failed (hash: {hashAlgorithm.Name}).");
        }

        return new AdkVerificationResult(
            chainValid,
            signatureValid,
            errors,
            envelope.Payload,
            payloadJson,
            envelope.LeafCertificate);
    }

    private bool TryValidateChain(AdkEnvelope envelope, List<string> errors)
    {
        if (_options.TrustedRoots.Count == 0)
        {
            errors.Add("No trusted root certificates were provided.");
            return false;
        }

        var leaf = envelope.LeafCertificate;
        using var chain = new X509Chain
        {
            ChainPolicy =
            {
                VerificationTime = _options.EvaluationTime.UtcDateTime,
                RevocationMode = X509RevocationMode.NoCheck,
                RevocationFlag = X509RevocationFlag.ExcludeRoot,
                VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
                TrustMode = X509ChainTrustMode.CustomRootTrust
            }
        };

        foreach (var root in _options.TrustedRoots)
        {
            chain.ChainPolicy.CustomTrustStore.Add(root);
        }

        for (var i = 1; i < envelope.Certificates.Count; i++)
        {
            chain.ChainPolicy.ExtraStore.Add(envelope.Certificates[i]);
        }

        var buildResult = chain.Build(leaf);
        if (!buildResult)
        {
            foreach (var status in chain.ChainStatus)
            {
                errors.Add(status.StatusInformation.Trim());
            }
        }

        return buildResult;
    }

    private bool TryValidateSignature(AdkEnvelope envelope, List<string> errors, out HashAlgorithmName algorithm)
    {
        algorithm = ResolveHashAlgorithm(envelope.Algorithm, errors);
        var leaf = envelope.LeafCertificate;
        try
        {
            if (leaf.GetRSAPublicKey() is RSA rsa)
            {
                return rsa.VerifyData(envelope.Payload, envelope.Signature, algorithm, RSASignaturePadding.Pkcs1);
            }

            if (leaf.GetECDsaPublicKey() is ECDsa ecdsa)
            {
                return ecdsa.VerifyData(envelope.Payload, envelope.Signature, algorithm);
            }

            errors.Add($"Unsupported public key algorithm: {leaf.PublicKey.Oid?.FriendlyName}");
            return false;
        }
        catch (CryptographicException ex)
        {
            errors.Add($"Cryptographic failure: {ex.Message}");
            return false;
        }
    }

    private HashAlgorithmName ResolveHashAlgorithm(string? algorithm, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            return _options.DefaultHashAlgorithm;
        }

        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => HashAlgorithmName.SHA256,
            "SHA384" => HashAlgorithmName.SHA384,
            "SHA512" => HashAlgorithmName.SHA512,
            _ => UnknownAlgorithmFallback(algorithm, errors)
        };
    }

    private HashAlgorithmName UnknownAlgorithmFallback(string name, List<string> errors)
    {
        errors.Add($"Unknown hash algorithm '{name}', falling back to {_options.DefaultHashAlgorithm.Name}.");
        return _options.DefaultHashAlgorithm;
    }

    private static JsonDocument? TryParseJson(byte[] payload, List<string> errors)
    {
        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            errors.Add("Payload is not valid JSON; returning raw bytes.");
            return null;
        }
    }
}
