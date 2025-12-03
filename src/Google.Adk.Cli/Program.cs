using System.Security.Cryptography.X509Certificates;
using Google.Adk;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return;
}

var command = args[0];
if (command.Equals("verify", StringComparison.OrdinalIgnoreCase))
{
    RunVerify(args.Skip(1).ToArray());
    return;
}

Console.Error.WriteLine($"Unknown command '{command}'.");
PrintHelp();

static void RunVerify(string[] args)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("verify requires an envelope path.");
        return;
    }

    var envelopePath = args[0];
    var rootPaths = new List<string>();
    var evaluationTime = DateTimeOffset.UtcNow;
    var hashAlgorithm = HashAlgorithmName.SHA256;

    for (var i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--root":
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("--root requires a path argument.");
                    return;
                }

                rootPaths.Add(args[++i]);
                break;
            case "--time":
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("--time requires an ISO-8601 timestamp.");
                    return;
                }

                if (!DateTimeOffset.TryParse(args[++i], out evaluationTime))
                {
                    Console.Error.WriteLine("Unable to parse --time value.");
                    return;
                }
                break;
            case "--hash":
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("--hash requires an algorithm.");
                    return;
                }

                hashAlgorithm = ParseHash(args[++i]);
                break;
        }
    }

    if (rootPaths.Count == 0)
    {
        Console.Error.WriteLine("At least one --root certificate is required.");
        return;
    }

    var options = new AdkVerifierOptions
    {
        EvaluationTime = evaluationTime,
        DefaultHashAlgorithm = hashAlgorithm
    };

    foreach (var root in rootPaths)
    {
        options.TrustedRoots.Add(CertificateLoader.LoadCertificateFile(root));
    }

    var envelope = AdkEnvelopeSerializer.ReadFromFile(envelopePath);
    var verifier = new AdkVerifier(options);
    var result = verifier.Verify(envelope);

    Console.WriteLine($"Chain valid: {result.IsChainValid}");
    Console.WriteLine($"Signature valid: {result.IsSignatureValid}");
    Console.WriteLine($"Overall valid: {result.IsValid}");
    Console.WriteLine($"Leaf subject: {result.LeafCertificate.Subject}");

    if (result.PayloadJson is not null)
    {
        Console.WriteLine("Payload JSON:");
        Console.WriteLine(result.PayloadJson.RootElement.GetRawText());
    }
    else
    {
        Console.WriteLine($"Payload (base64): {Convert.ToBase64String(result.Payload)}");
    }

    if (result.Errors.Count > 0)
    {
        Console.WriteLine("Errors:");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($" - {error}");
        }
    }
}

static HashAlgorithmName ParseHash(string input)
{
    return input.ToUpperInvariant() switch
    {
        "SHA256" => HashAlgorithmName.SHA256,
        "SHA384" => HashAlgorithmName.SHA384,
        "SHA512" => HashAlgorithmName.SHA512,
        _ => HashAlgorithmName.SHA256
    };
}

static void PrintHelp()
{
    Console.WriteLine("Usage: adk verify <envelope.json> --root <certificate.pem> [--time <ISO-8601>] [--hash <SHA256|SHA384|SHA512>]");
}
