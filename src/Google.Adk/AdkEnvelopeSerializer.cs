using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Google.Adk;

/// <summary>
/// Helpers for translating envelopes to and from JSON.
/// </summary>
public static class AdkEnvelopeSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Deserialize a JSON envelope into a strongly typed representation.
    /// </summary>
    /// <exception cref="InvalidDataException">The input JSON was malformed or missing fields.</exception>
    public static AdkEnvelope Deserialize(string json)
    {
        var model = JsonSerializer.Deserialize<AdkEnvelopeModel>(json, SerializerOptions)
            ?? throw new InvalidDataException("Envelope JSON could not be parsed.");

        if (string.IsNullOrWhiteSpace(model.Payload))
        {
            throw new InvalidDataException("Envelope is missing payload content.");
        }

        if (string.IsNullOrWhiteSpace(model.Signature))
        {
            throw new InvalidDataException("Envelope is missing a signature.");
        }

        if (model.Certificates.Count == 0)
        {
            throw new InvalidDataException("Envelope must include at least one certificate.");
        }

        var payload = Convert.FromBase64String(model.Payload);
        var signature = Convert.FromBase64String(model.Signature);
        var certificates = model.Certificates
            .Select(CertificateLoader.LoadCertificate)
            .ToArray();

        return new AdkEnvelope(payload, signature, certificates, model.Algorithm);
    }

    /// <summary>
    /// Serialize an envelope into JSON, emitting certificate data as PEM.
    /// </summary>
    public static string Serialize(AdkEnvelope envelope)
    {
        var model = new AdkEnvelopeModel
        {
            Payload = Convert.ToBase64String(envelope.Payload),
            Signature = Convert.ToBase64String(envelope.Signature),
            Certificates = envelope.Certificates
                .Select(CertificateLoader.ExportCertificateString)
                .ToList(),
            Algorithm = envelope.Algorithm
        };

        return JsonSerializer.Serialize(model, SerializerOptions);
    }

    /// <summary>
    /// Read an envelope from a file path.
    /// </summary>
    public static AdkEnvelope ReadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }
}
