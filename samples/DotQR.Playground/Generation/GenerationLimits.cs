namespace DotQR.Playground.Generation;

public static class GenerationLimits
{
    public const int MaxTextCharacters = 8192;
    public const int MaxPixelsPerModule = 32;
    public const int MaxOutputSide = 2048;
    public const int MaxUploadBytes = 2 * 1024 * 1024;
    public const int MaxUploadSide = 2048;
    public const int MaxOutputBytes = 16 * 1024 * 1024;

    public static void Validate(RendererOptions options, int moduleCount)
    {
        if (!Enum.IsDefined(options.Kind)) throw new ArgumentException("Select a supported renderer.");
        if (options.HasScale)
        {
            if (options.PixelsPerModule is < 1 or > MaxPixelsPerModule) throw new ArgumentException("Scale must be 1–32 pixels per module.");
            if ((long)moduleCount * options.PixelsPerModule > MaxOutputSide) throw new ArgumentException("Output must be at most 2048 pixels/points per side. Reduce scale or QR version.");
        }
        if (options.Kind is RendererKind.Svg or RendererKind.Postscript && options.ExplicitSize)
        {
            if (options.Width is < 1 or > MaxOutputSide || options.Height is < 1 or > MaxOutputSide) throw new ArgumentException("Vector dimensions must be 1–2048 pixels/points.");
        }
        if (options.Kind == RendererKind.Pdf && options.Dpi is < 36 or > 1200) throw new ArgumentException("PDF DPI must be 36–1200.");
        if (options.Kind == RendererKind.Art && (!double.IsFinite(options.PixelSizeFactor) || options.PixelSizeFactor is <= 0 or > 1 || options.PixelsPerModule * options.PixelSizeFactor < 1))
            throw new ArgumentException("Art dot factor must be greater than 0 and at most 1, producing at least one pixel per module.");
        if (options.HasLogo && options.Logo is not null && (options.IconSizePercent is < 1 or > 40 || options.IconBorderWidth is < 0 or > 20))
            throw new ArgumentException("Logo coverage must be 1–40%; border must be 0–20 pixels.");
        if (options.Kind == RendererKind.Ascii && !options.Compact)
        {
            if (options.RepeatPerModule is < 1 or > 8 || options.DarkColorString.Length is < 1 or > 8 || options.DarkColorString.Length != options.WhiteSpaceString.Length)
                throw new ArgumentException("ASCII needs equally long module strings (1–8 characters) and repetition 1–8.");
            if (options.DarkColorString.Any(char.IsControl) || options.WhiteSpaceString.Any(char.IsControl)) throw new ArgumentException("ASCII module strings cannot contain control characters.");
        }
        if (options.Kind == RendererKind.Ascii && options.EndOfLine is not ("\n" or "\r\n")) throw new ArgumentException("Select LF or CRLF line endings.");
        foreach (var image in new[] { options.Logo, options.BackgroundImage, options.FinderPatternImage })
            if (image?.Length > MaxUploadBytes) throw new ArgumentException("Images must be at most 2 MiB.");
    }

    public static void ValidateText(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > MaxTextCharacters) throw new ArgumentException("Payload must contain 1–8192 characters. QR capacity may be lower.");
    }
}
