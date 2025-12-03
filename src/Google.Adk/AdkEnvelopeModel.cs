using System.Text.Json.Serialization;

namespace Google.Adk;

/// <summary>
/// JSON representation of an ADK envelope.
/// </summary>
public sealed class AdkEnvelopeModel
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("certificates")]
    public List<string> Certificates { get; set; } = new();

    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }
}
