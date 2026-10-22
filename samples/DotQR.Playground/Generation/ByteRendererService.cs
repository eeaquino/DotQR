namespace DotQR.Playground.Generation;

internal sealed record RenderedOutput(byte[] Content, string FileName, string MimeType,
    string? PreviewDataUri = null, string? TextPreview = null, string? PreviewNotice = null)
{
    internal static RenderedOutput Image(byte[] bytes, string extension, string mime) =>
        new(bytes, "dotqr." + extension, mime, DataUri(bytes, mime));
    internal static RenderedOutput Text(string text, string extension, string mime, bool image = false) =>
        new(Encoding.UTF8.GetBytes(text), "dotqr." + extension, mime,
            image ? DataUri(Encoding.UTF8.GetBytes(text), mime) : null, image ? null : text);
    internal static string DataUri(byte[] bytes, string mime) => $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
}

internal static class ByteRendererService
{
    // Renderers own their data. The caller captures metadata before handing it off.
    internal static RenderedOutput Render(QRCodeData data, RendererOptions o)
    {
        switch (o.Kind)
        {
            case RendererKind.Png:
                using (var qr = new PngByteQRCode(data))
                    return RenderedOutput.Image(qr.GetGraphic(o.PixelsPerModule, o.Dark, o.Light, o.DrawQuietZones), "png", "image/png");
            case RendererKind.Bitmap:
                using (var qr = new BitmapByteQRCode(data))
                    return RenderedOutput.Image(qr.GetGraphic(o.PixelsPerModule, Hex(o.Dark), Hex(o.Light)), "bmp", "image/bmp");
            case RendererKind.Pdf:
                var preview = MatrixPreview(data, o);
                using (var qr = new PdfByteQRCode(data))
                    return new(qr.GetGraphic(o.PixelsPerModule, Hex(o.Dark), Hex(o.Light), o.Dpi), "dotqr.pdf", "application/pdf", preview,
                        PreviewNotice: "PNG matrix preview only; download contains the actual PDF with the selected DPI.");
            case RendererKind.Svg:
                using (var qr = new SvgQRCode(data))
                {
                    var logo = o.Logo is null ? null : new SvgQRCode.SvgLogo(o.Logo, o.IconSizePercent, o.FillLogoBackground);
                    var svg = o.ExplicitSize
                        ? qr.GetGraphic(new Size(o.Width, o.Height), o.Dark, o.Light, o.DrawQuietZones, o.SizingMode, logo)
                        : qr.GetGraphic(o.PixelsPerModule, o.Dark, o.Light, o.DrawQuietZones, o.SizingMode, logo);
                    return RenderedOutput.Text(svg, "svg", "image/svg+xml", true);
                }
            case RendererKind.Postscript:
                using (var qr = new PostscriptQRCode(data))
                {
                    var ps = o.ExplicitSize
                        ? qr.GetGraphic(new Size(o.Width, o.Height), o.Dark, o.Light, o.DrawQuietZones, o.EpsFormat)
                        : qr.GetGraphic(o.PixelsPerModule, o.Dark, o.Light, o.DrawQuietZones, o.EpsFormat);
                    return RenderedOutput.Text(ps, o.EpsFormat ? "eps" : "ps", "application/postscript");
                }
            case RendererKind.Ascii:
                using (var qr = new ASCIIQRCode(data))
                    return RenderedOutput.Text(o.Compact ? qr.GetGraphicSmall(o.DrawQuietZones, o.Invert, o.EndOfLine)
                        : qr.GetGraphic(o.RepeatPerModule, o.DarkColorString, o.WhiteSpaceString, o.DrawQuietZones, o.EndOfLine), "txt", "text/plain;charset=utf-8");
            default:
                throw new ArgumentException("Select a dependency-free renderer.");
        }
    }

    private static string MatrixPreview(QRCodeData data, RendererOptions o)
    {
        using var clone = new QRCodeData(data.GetRawData(QRCodeData.Compression.Uncompressed), QRCodeData.Compression.Uncompressed);
        using var png = new PngByteQRCode(clone);
        return RenderedOutput.DataUri(png.GetGraphic(o.PixelsPerModule, o.Dark, o.Light), "image/png");
    }
    private static string Hex(RgbaColor color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
