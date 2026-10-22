using SkiaSharp;

namespace DotQR.Playground.Generation;

public static class UploadImageReader
{
    public static async Task<byte[]> ReadAsync(Stream stream, long declaredLength, CancellationToken cancellationToken = default)
    {
        if (declaredLength is < 1 or > GenerationLimits.MAX_UPLOAD_BYTES)
            throw new ArgumentException("Choose a PNG, JPEG, or WebP image of at most 2 MiB.");
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int length;
        while ((length = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + length > GenerationLimits.MAX_UPLOAD_BYTES)
                throw new ArgumentException("Image exceeds 2 MiB.");
            await buffer.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = Decode(buffer.ToArray());
        // Normalize to PNG: SvgLogo's byte overload declares PNG regardless of input format.
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100) ?? throw new ArgumentException("Image could not be encoded.");
        if (encoded.Size > GenerationLimits.MAX_UPLOAD_BYTES)
            throw new ArgumentException("Normalized PNG exceeds 2 MiB. Use a smaller image.");
        return encoded.ToArray();
    }

    internal static SKBitmap Decode(byte[] bytes)
    {
        if (bytes.Length is < 1 or > GenerationLimits.MAX_UPLOAD_BYTES)
            throw new ArgumentException("Image must be 1 byte–2 MiB.");
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new ArgumentException("Invalid raster image. Choose PNG, JPEG, or WebP; SVG uploads are not supported.");
        if (codec.EncodedFormat is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp))
            throw new ArgumentException("Only PNG, JPEG, and WebP uploads are accepted.");
        var width = codec.Info.Width;
        var height = codec.Info.Height;
        if (width is < 1 or > GenerationLimits.MAX_UPLOAD_SIDE || height is < 1 or > GenerationLimits.MAX_UPLOAD_SIDE || (long)width * height > 2048L * 2048)
            throw new ArgumentException("Image dimensions must not exceed 2048 × 2048 pixels.");
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            bitmap.Dispose();
            throw new ArgumentException("Image data is incomplete or cannot be decoded.");
        }
        return bitmap;
    }
}
