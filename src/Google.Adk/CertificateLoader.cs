using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Google.Adk;

/// <summary>
/// Utilities for working with certificate encodings.
/// </summary>
public static class CertificateLoader
{
    private const string CertificateHeader = "-----BEGIN CERTIFICATE-----";
    private const string CertificateFooter = "-----END CERTIFICATE-----";

    public static X509Certificate2 LoadCertificate(string encoded)
    {
        if (encoded.Contains(CertificateHeader, StringComparison.Ordinal))
        {
            var pemBytes = DecodePem(encoded);
            return new X509Certificate2(pemBytes);
        }

        return new X509Certificate2(Convert.FromBase64String(encoded));
    }

    public static X509Certificate2 LoadCertificateFile(string path)
    {
        var content = File.ReadAllText(path);
        return LoadCertificate(content);
    }

    public static string ExportCertificateString(X509Certificate2 certificate)
    {
        var base64 = Convert.ToBase64String(certificate.RawData, Base64FormattingOptions.InsertLineBreaks);
        var builder = new StringBuilder();
        builder.AppendLine(CertificateHeader);
        builder.AppendLine(base64);
        builder.AppendLine(CertificateFooter);
        return builder.ToString();
    }

    private static byte[] DecodePem(string pem)
    {
        var builder = new StringBuilder();
        foreach (var line in pem.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            builder.Append(trimmed);
        }

        return Convert.FromBase64String(builder.ToString());
    }
}
