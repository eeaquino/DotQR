namespace DotQR.Playground.Generation;

public sealed record EncodingOptions
{
    public bool IsMicro { get; init; }
    public int Version { get; init; } = -1;
    public QRCodeGenerator.ECCLevel EccLevel { get; init; } = QRCodeGenerator.ECCLevel.M;
    public bool ForceUtf8 { get; init; }
    public bool Utf8Bom { get; init; }
    public QRCodeGenerator.EciMode EciMode { get; init; } = QRCodeGenerator.EciMode.Default;
}
