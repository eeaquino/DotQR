namespace DotQR.Playground.Generation;

public enum RendererKind { Png, Bitmap, Pdf, Svg, Postscript, Ascii, Skia, Art, Base64 }

public sealed record RendererOptions
{
    public RendererKind Kind { get; init; }
    public int PixelsPerModule { get; init; } = 8;
    public string DarkColor { get; init; } = "#111827";
    public string LightColor { get; init; } = "#ffffff";
    public byte DarkAlpha { get; init; } = 255;
    public byte LightAlpha { get; init; } = 255;
    public bool DrawQuietZones { get; init; } = true;
    public bool ExplicitSize { get; init; }
    public int Width { get; init; } = 512;
    public int Height { get; init; } = 512;
    public SvgQRCode.SizingMode SizingMode { get; init; }
    public int Dpi { get; init; } = 150;
    public bool EpsFormat { get; init; }
    public bool Compact { get; init; }
    public bool Invert { get; init; }
    public int RepeatPerModule { get; init; } = 1;
    public string DarkColorString { get; init; } = "██";
    public string WhiteSpaceString { get; init; } = "  ";
    public string EndOfLine { get; init; } = "\n";
    public Base64QRCode.ImageType ImageType { get; init; } = Base64QRCode.ImageType.Png;
    public byte[]? Logo { get; init; }
    public int IconSizePercent { get; init; } = 15;
    public int IconBorderWidth { get; init; } = 2;
    public string IconBackgroundColor { get; init; } = "#ffffff";
    public bool FillLogoBackground { get; init; } = true;
    public string BackgroundColor { get; init; } = "#ffffff";
    public double PixelSizeFactor { get; init; } = 0.8;
    public ArtQRCode.QuietZoneStyle QuietZoneRenderingStyle { get; init; } = ArtQRCode.QuietZoneStyle.Dotted;
    public ArtQRCode.BackgroundImageStyle BackgroundImageStyle { get; init; } = ArtQRCode.BackgroundImageStyle.DataAreaOnly;
    public byte[]? BackgroundImage { get; init; }
    public byte[]? FinderPatternImage { get; init; }

    public bool HasScale => Kind != RendererKind.Ascii && !((Kind is RendererKind.Svg or RendererKind.Postscript) && ExplicitSize)
        && !(Kind == RendererKind.Svg && SizingMode == SvgQRCode.SizingMode.ViewBoxAttribute);
    public bool HasColors => Kind != RendererKind.Ascii;
    public bool HasAlpha => Kind is RendererKind.Png or RendererKind.Svg or RendererKind.Skia or RendererKind.Art || (Kind == RendererKind.Base64 && ImageType != Base64QRCode.ImageType.Jpeg);
    public bool HasQuietZones => Kind is not (RendererKind.Bitmap or RendererKind.Pdf);
    public bool HasLogo => Kind is RendererKind.Svg or RendererKind.Skia or RendererKind.Base64;
    public RendererOptions Snapshot() => this with { Logo = Logo?.ToArray(), BackgroundImage = BackgroundImage?.ToArray(), FinderPatternImage = FinderPatternImage?.ToArray() };
    public RgbaColor Dark => Color(DarkColor, HasAlpha ? DarkAlpha : (byte)255);
    public RgbaColor Light => Color(LightColor, HasAlpha ? LightAlpha : (byte)255);
    private static RgbaColor Color(string value, byte alpha)
    {
        var color = RgbaColor.Parse(value);
        return new(color.R, color.G, color.B, alpha);
    }
}
