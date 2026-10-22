namespace DotQR.Playground.Generation;

public sealed record GenerationResult(byte[] Content, string FileName, string MimeType,
    string? PreviewDataUri, string? TextPreview, string PayloadText, int Version, int ModuleCount,
    string EffectiveEncoding, string? PreviewNotice);

public sealed record GenerationOutcome(GenerationResult? Result, string? Error);
