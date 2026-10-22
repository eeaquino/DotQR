using SkiaSharp;

namespace DotQR.Playground.Generation;

internal static class SkiaRendererService
{
    internal static RenderedOutput Render(QRCodeData data, RendererOptions o)
    {
        using var logo = o.HasLogo && o.Logo is not null ? UploadImageReader.Decode(o.Logo) : null;
        var dark = Color(o.Dark);
        var light = Color(o.Light);
        switch (o.Kind)
        {
            case RendererKind.Skia:
                using (var qr = new QRCode(data))
                using (var bitmap = qr.GetGraphic(o.PixelsPerModule, dark, light, logo, o.IconSizePercent, o.IconBorderWidth, o.DrawQuietZones, SKColor.Parse(o.IconBackgroundColor)))
                    return Png(bitmap);
            case RendererKind.Art:
                using (var background = o.BackgroundImage is null ? null : UploadImageReader.Decode(o.BackgroundImage))
                using (var finder = o.FinderPatternImage is null ? null : UploadImageReader.Decode(o.FinderPatternImage))
                using (var qr = new ArtQRCode(data))
                using (var bitmap = qr.GetGraphic(o.PixelsPerModule, o.FinderPatternShape, dark, light, SKColor.Parse(o.BackgroundColor), background, o.PixelSizeFactor,
                    o.DrawQuietZones, o.QuietZoneRenderingStyle, o.BackgroundImageStyle, finder))
                    return Png(bitmap);
            case RendererKind.Base64:
                using (var qr = new Base64QRCode(data))
                {
                    var text = logo is null
                        ? qr.GetGraphic(o.PixelsPerModule, dark, light, o.DrawQuietZones, o.ImageType)
                        : qr.GetGraphic(o.PixelsPerModule, dark, light, logo, o.IconSizePercent, o.IconBorderWidth, o.DrawQuietZones, o.ImageType);
                    var mime = o.ImageType switch
                    {
                        Base64QRCode.ImageType.Png => "image/png",
                        Base64QRCode.ImageType.Jpeg => "image/jpeg",
                        Base64QRCode.ImageType.Webp => "image/webp",
                        _ => throw new ArgumentException("Choose PNG, JPEG, or WebP.")
                    };
                    return new(Encoding.UTF8.GetBytes(text), "dotqr-base64.txt", "text/plain;charset=utf-8", $"data:{mime};base64,{text}", text,
                        "Base64 text download; the image preview uses the selected encoded image format.");
                }
            default:
                throw new ArgumentException("Select a Skia renderer.");
        }
    }

    private static SKColor Color(RgbaColor color) => new(color.R, color.G, color.B, color.A);
    private static RenderedOutput Png(SKBitmap bitmap)
    {
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidOperationException("PNG encoding is unavailable.");
        return RenderedOutput.Image(encoded.ToArray(), "png", "image/png");
    }
}
