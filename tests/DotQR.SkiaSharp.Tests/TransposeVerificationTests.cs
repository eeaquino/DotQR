using SkiaSharp;

namespace DotQRTests.SkiaSharp;

/// <summary>
/// Tests to verify that QR codes are not transposed along the main diagonal.
/// Human verification of approval files is required to ensure correct orientation.
/// </summary>
public class TransposeVerificationTests
{
    private readonly QRCodeData _sharedQrCodeData;

    public TransposeVerificationTests()
    {
        // A distinctive pattern that makes transpose errors obvious.
        var gen = new QRCodeGenerator();
        _sharedQrCodeData = gen.CreateQrCode("ABCD", QRCodeGenerator.ECCLevel.L);
    }

    [Theory]
    [InlineData("QRCode")]
    [InlineData("BitmapByteQRCode")]
    [InlineData("Base64QRCode")]
    [InlineData("PngByteQRCode")]
    public void image_renderers(string rendererType)
    {
        var imageBytes = rendererType switch
        {
            "QRCode" => EncodeQrCode(),
            "BitmapByteQRCode" => new BitmapByteQRCode(_sharedQrCodeData).GetGraphic(10),
            "Base64QRCode" => Convert.FromBase64String(new Base64QRCode(_sharedQrCodeData).GetGraphic(10)),
            "PngByteQRCode" => new PngByteQRCode(_sharedQrCodeData).GetGraphic(10),
            _ => throw new ArgumentException($"Unknown renderer type: {rendererType}", nameof(rendererType)),
        };

        using var bitmap = SKBitmap.Decode(imageBytes)
            ?? throw new InvalidOperationException($"Could not decode {rendererType} output.");
        bitmap.ShouldMatchApproved(discriminator: rendererType);
    }

    private byte[] EncodeQrCode()
    {
        using var bitmap = new QRCode(_sharedQrCodeData).GetGraphic(10);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void artqrcode_renderer()
    {
        using var bitmap = new ArtQRCode(_sharedQrCodeData).GetGraphic(
            10,
            SKColors.Black,
            SKColors.White,
            SKColors.White,
            null,
            1,
            true,
            ArtQRCode.QuietZoneStyle.Flat,
            ArtQRCode.BackgroundImageStyle.Fill,
            null);
        bitmap.ShouldMatchApproved();
    }

    [Fact]
    public void black_module_position()
    {
        // The black module in a QR code is always at position (4*version + 9, 8) + (4,4) due to the quiet zone.
        // For a version 1 QR code it should be at (17, 12); if transposed it would be at (12, 17).
        var version = _sharedQrCodeData.Version;
        var expectedRow = 4 * version + 9 + 4;
        var expectedCol = 8 + 4;
        var transposedRow = expectedCol;
        var transposedCol = expectedRow;

        _sharedQrCodeData.ModuleMatrix[expectedRow][expectedCol].ShouldBeTrue(
            $"Black module should be at position ({expectedRow}, {expectedCol})");

        if (expectedRow != transposedCol || expectedCol != transposedRow)
        {
            _sharedQrCodeData.ModuleMatrix[transposedRow][transposedCol].ShouldBeFalse(
                $"Black module should NOT be at transposed position ({transposedRow}, {transposedCol})");
        }
    }

    [Fact]
    public void black_module_reference()
    {
        // Render a matrix containing only the black module, for human verification of its position.
        var version = _sharedQrCodeData.Version;
        var blackModuleRow = 4 * version + 9 + 4;
        var blackModuleCol = 8 + 4;

        using var referenceData = new QRCodeData(version, addPadding: true);
        referenceData.ModuleMatrix[blackModuleRow][blackModuleCol] = true;

        using var bitmap = new QRCode(referenceData).GetGraphic(10);
        bitmap.ShouldMatchApproved();
    }
}
