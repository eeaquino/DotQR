using G = DotQR.PayloadGenerator.Girocode;

namespace DotQR.Playground.Generation;

public static class GirocodeEncodingPolicy
{
    public static QRCodeGenerator.EciMode? EciFor(G.GirocodeEncoding charset) => charset switch
    {
        G.GirocodeEncoding.UTF_8 => QRCodeGenerator.EciMode.Utf8,
        G.GirocodeEncoding.ISO_8859_1 => QRCodeGenerator.EciMode.Iso8859_1,
        G.GirocodeEncoding.ISO_8859_2 => QRCodeGenerator.EciMode.Iso8859_2,
        _ => null
    };

    internal static QRCodeData Generate(QRCodeGenerator generator, string text, G.GirocodeEncoding charset, int version, out string effective)
    {
        if (EciFor(charset) is { } eci)
        {
            effective = $"Girocode ECC M; payload charset {charset}; ECI {eci}; BOM off; requested QR version {(version == -1 ? "automatic" : version.ToString(CultureInfo.InvariantCulture))}";
            return generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M, eciMode: eci, requestedVersion: version);
        }
        // The public binary entry point supports exact legacy bytes, but not a requested version or ECI.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(charset.ToString().Replace('_', '-'));
        effective = $"Girocode ECC M; binary {charset} payment bytes; automatic version; no ECI (charset is declared in payment header)";
        return generator.CreateQrCode(encoding.GetBytes(text), QRCodeGenerator.ECCLevel.M);
    }
}
