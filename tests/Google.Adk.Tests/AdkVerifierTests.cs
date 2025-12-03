using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Google.Adk;
using Xunit;

namespace Google.Adk.Tests;

public class AdkVerifierTests
{
    [Fact]
    public void Verify_Succeeds_WithValidChainAndSignature()
    {
        var payload = Encoding.UTF8.GetBytes("{\"nonce\":123,\"device\":\"demo\"}");
        CreateCertificateChain(out var root, out var intermediate, out var leaf, out var leafKey);

        var signature = leafKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var envelope = new AdkEnvelope(payload, signature, new[] { leaf, intermediate, root }, "SHA256");

        var options = new AdkVerifierOptions();
        options.TrustedRoots.Add(root);
        var verifier = new AdkVerifier(options);

        var result = verifier.Verify(envelope);

        Assert.True(result.IsValid);
        Assert.True(result.IsChainValid);
        Assert.True(result.IsSignatureValid);
        Assert.NotNull(result.PayloadJson);
    }

    [Fact]
    public void Verify_Fails_ForTamperedPayload()
    {
        var payload = Encoding.UTF8.GetBytes("{\"nonce\":123,\"device\":\"demo\"}");
        CreateCertificateChain(out var root, out var intermediate, out var leaf, out var leafKey);

        var signature = leafKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var tampered = Encoding.UTF8.GetBytes("{\"nonce\":999\"device\":\"demo\"}");
        var envelope = new AdkEnvelope(tampered, signature, new[] { leaf, intermediate, root }, null);

        var options = new AdkVerifierOptions();
        options.TrustedRoots.Add(root);
        var verifier = new AdkVerifier(options);

        var result = verifier.Verify(envelope);

        Assert.False(result.IsValid);
        Assert.False(result.IsSignatureValid);
    }

    [Fact]
    public void Serializer_RoundTripsEnvelope()
    {
        var payload = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
        CreateCertificateChain(out var root, out var intermediate, out var leaf, out var leafKey);
        var signature = leafKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var envelope = new AdkEnvelope(payload, signature, new[] { leaf, intermediate, root }, "SHA256");

        var json = AdkEnvelopeSerializer.Serialize(envelope);
        var roundTripped = AdkEnvelopeSerializer.Deserialize(json);

        Assert.Equal(envelope.Payload, roundTripped.Payload);
        Assert.Equal(envelope.Signature, roundTripped.Signature);
        Assert.Equal(envelope.Certificates.Count, roundTripped.Certificates.Count);
    }

    [Fact]
    public void Verify_UsesDefaultHashAlgorithm_WhenEnvelopeOmitsAlgorithm()
    {
        var payload = Encoding.UTF8.GetBytes("{\"message\":\"hash override\"}");
        CreateCertificateChain(out var root, out var intermediate, out var leaf, out var leafKey);

        var signature = leafKey.SignData(payload, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        var envelope = new AdkEnvelope(payload, signature, new[] { leaf, intermediate, root }, null);

        var options = new AdkVerifierOptions { DefaultHashAlgorithm = HashAlgorithmName.SHA512 };
        options.TrustedRoots.Add(root);

        var verifier = new AdkVerifier(options);
        var result = verifier.Verify(envelope);

        Assert.True(result.IsSignatureValid);
        Assert.True(result.IsChainValid);
    }

    [Fact]
    public void Verify_ReportsUnknownHashAlgorithm()
    {
        var payload = Encoding.UTF8.GetBytes("{\"message\":\"unknown hash\"}");
        CreateCertificateChain(out var root, out var intermediate, out var leaf, out var leafKey);

        var signature = leafKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var envelope = new AdkEnvelope(payload, signature, new[] { leaf, intermediate, root }, "MD5");

        var options = new AdkVerifierOptions { DefaultHashAlgorithm = HashAlgorithmName.SHA256 };
        options.TrustedRoots.Add(root);

        var verifier = new AdkVerifier(options);
        var result = verifier.Verify(envelope);

        Assert.Contains(result.Errors, e => e.Contains("Unknown hash algorithm"));
    }

    [Fact]
    public void Verify_Fails_WhenNoTrustedRootsProvided()
    {
        var payload = Encoding.UTF8.GetBytes("{\"message\":\"no trust\"}");
        CreateCertificateChain(out _, out var intermediate, out var leaf, out var leafKey);

        var signature = leafKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var envelope = new AdkEnvelope(payload, signature, new[] { leaf, intermediate }, "SHA256");

        var verifier = new AdkVerifier(new AdkVerifierOptions());
        var result = verifier.Verify(envelope);

        Assert.False(result.IsChainValid);
        Assert.Contains(result.Errors, e => e.Contains("No trusted root"));
    }

    [Fact]
    public void Envelope_Constructor_ValidatesInputs()
    {
        var payload = Encoding.UTF8.GetBytes("data");
        var signature = Encoding.UTF8.GetBytes("sig");
        CreateCertificateChain(out var root, out var intermediate, out var leaf, out var leafKey);

        Assert.Throws<ArgumentException>(() => new AdkEnvelope(Array.Empty<byte>(), signature, new[] { leaf, intermediate, root }, null));
        Assert.Throws<ArgumentException>(() => new AdkEnvelope(payload, Array.Empty<byte>(), new[] { leaf, intermediate, root }, null));
        Assert.Throws<ArgumentException>(() => new AdkEnvelope(payload, signature, Array.Empty<X509Certificate2>(), null));
    }

    private static void CreateCertificateChain(
        out X509Certificate2 root,
        out X509Certificate2 intermediate,
        out X509Certificate2 leaf,
        out RSA leafKey)
    {
        using var rootKey = RSA.Create(2048);
        root = CreateCertificate("CN=ADK Root", rootKey, null, null, true, TimeSpan.FromDays(3650));

        using var intermediateKey = RSA.Create(2048);
        intermediate = CreateCertificate("CN=ADK Intermediate", intermediateKey, root, rootKey, true, TimeSpan.FromDays(1825));

        leafKey = RSA.Create(2048);
        leaf = CreateCertificate("CN=ADK Leaf", leafKey, intermediate, intermediateKey, false, TimeSpan.FromDays(365));
    }

    private static X509Certificate2 CreateCertificate(
        string subject,
        RSA key,
        X509Certificate2? issuer,
        RSA? issuerKey,
        bool isCa,
        TimeSpan validity)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(isCa, false, 0, true));
        var usage = isCa ? X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign : X509KeyUsageFlags.DigitalSignature;
        request.CertificateExtensions.Add(new X509KeyUsageExtension(usage, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore + validity;

        X509Certificate2 certificate;
        if (issuer is null)
        {
            certificate = request.CreateSelfSigned(notBefore, notAfter);
        }
        else
        {
            var serial = Guid.NewGuid().ToByteArray();
            certificate = request.Create(
                issuer.SubjectName,
                X509SignatureGenerator.CreateForRSA(issuerKey!, RSASignaturePadding.Pkcs1),
                notBefore,
                notAfter,
                serial);
        }

        // Certificates created with CertificateRequest already have an associated private key.
        // Only attach the key when necessary to avoid InvalidOperationException.
        return certificate.HasPrivateKey ? certificate : certificate.CopyWithPrivateKey(key);
    }
}
