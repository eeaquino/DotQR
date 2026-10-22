using DotQR.Exceptions;
using P = DotQR.PayloadGenerator;

namespace DotQR.Playground.Generation;

public sealed class QrGenerationService(ILogger<QrGenerationService> logger)
{
    static QrGenerationService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public GenerationOutcome Generate(GenerationRequest request)
    {
        try
        {
            request.Values.Validate(request.Definition.Fields);
            var payload = request.Definition.Create(request.Values);
            var text = payload?.ToString() ?? request.Values.Text("text");
            GenerationLimits.ValidateText(text);
            var encoding = request.Encoding;
            if (encoding.IsMicro && request.Definition.Id != "text")
                throw new ArgumentException("Micro QR is available for plain text only.");
            if (encoding.IsMicro && request.Renderer.Kind == RendererKind.Art)
                throw new ArgumentException("ArtQRCode assumes standard QR finder patterns. Choose another renderer for Micro QR.");
            if (encoding.IsMicro && encoding.Version is < -4 or > -1)
                throw new ArgumentException("Choose Micro version M1–M4.");
            if (!encoding.IsMicro && encoding.Version is not (-1 or >= 1 and <= 40))
                throw new ArgumentException("Choose automatic version or standard version 1–40.");
            using var generator = new QRCodeGenerator();
            using var data = payload is P.Girocode
                ? GirocodeEncodingPolicy.Generate(generator, text, request.Values.Enum<P.Girocode.GirocodeEncoding>("encoding"), encoding.Version, out var effective)
                : CreateData(generator, payload, text, encoding, out effective);
            var version = data.Version;
            var count = data.ModuleMatrix.Count;
            GenerationLimits.Validate(request.Renderer, count);
            var rendered = request.Renderer.Kind is RendererKind.Skia or RendererKind.Art or RendererKind.Base64
                ? SkiaRendererService.Render(data, request.Renderer)
                : ByteRendererService.Render(data, request.Renderer);
            if (rendered.Content.Length > GenerationLimits.MAX_OUTPUT_BYTES)
                throw new ArgumentException("Output exceeds 16 MiB. Reduce size or renderer complexity.");
            return new(new(rendered.Content, rendered.FileName, rendered.MimeType, rendered.PreviewDataUri, rendered.TextPreview,
                text, version, count, effective, rendered.PreviewNotice, request.Renderer.Kind == RendererKind.Art), null);
        }
        catch (Exception ex) when (IsInputError(ex))
        {
            return new(null, ex.Message);
        }
        catch (Exception ex)
        {
            // Payload exceptions may contain sensitive values. Log only the type, never the exception/message.
            PlaygroundLog.GenerationFailed(logger, ex.GetType().Name);
            return new(null, "QR generation failed. Check the selected settings and native SkiaSharp setup, then try again.");
        }
    }

    private static QRCodeData CreateData(QRCodeGenerator generator, P.Payload? payload, string text, EncodingOptions o, out string effective)
    {
        if (o.IsMicro)
        {
            var ecc = o.Version == -1 ? QRCodeGenerator.ECCLevel.Default : o.EccLevel == QRCodeGenerator.ECCLevel.Default ? QRCodeGenerator.ECCLevel.L : o.EccLevel;
            effective = $"Micro M{-o.Version}; ECC {ecc}; automatic content encoding";
            return QRCodeGenerator.GenerateMicroQrCode(text, o.EccLevel, o.Version);
        }
        var level = payload?.EccLevel is { } mandated && mandated != QRCodeGenerator.ECCLevel.Default ? mandated
            : o.EccLevel == QRCodeGenerator.ECCLevel.Default ? QRCodeGenerator.ECCLevel.M : o.EccLevel;
        if (payload is P.RussiaPaymentOrder russian)
        {
            effective = $"Binary payment bytes; ECC {level}; payload character set; automatic version (generic encoding controls do not apply)";
            return generator.CreateQrCode(russian.ToBytes(), level);
        }
        if (HasMandatedEncoding(payload))
        {
            effective = $"Payload-required encoding: ECC {level}; ECI {payload!.EciMode}; version {(payload.Version == -1 ? "automatic" : payload.Version.ToString(CultureInfo.InvariantCulture))}; UTF-8 flags off";
            return generator.CreateQrCode(payload, level);
        }
        effective = $"ECC {level}; ECI {o.EciMode}; UTF-8 {o.ForceUtf8}; BOM {o.Utf8Bom}";
        return generator.CreateQrCode(text, level, o.ForceUtf8, o.Utf8Bom, o.EciMode, o.Version);
    }

    public static bool HasMandatedEncoding(P.Payload? payload) => payload is not null &&
        (payload.Version != -1 || payload.EccLevel != QRCodeGenerator.ECCLevel.Default || payload.EciMode != QRCodeGenerator.EciMode.Default);

    private static bool IsInputError(Exception ex) => ex is ArgumentException or FormatException or OverflowException or InvalidDataException or InvalidOperationException or DataTooLongException
        or P.BezahlCode.BezahlCodeException or P.Girocode.GirocodeException or P.MoneroTransaction.MoneroTransactionException
        or P.RussiaPaymentOrder.RussiaPaymentOrderException or P.ShadowSocksConfig.ShadowSocksConfigException
        or P.SwissQrCode.SwissQrCodeException or P.SwissQrCode.Contact.SwissQrCodeContactException or P.SwissQrCode.Iban.SwissQrCodeIbanException
        or P.SwissQrCode.Reference.SwissQrCodeReferenceException or P.SwissQrCode.AdditionalInformation.SwissQrCodeAdditionalInformationException;
}
