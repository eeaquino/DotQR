namespace DotQR.Playground;

internal static partial class PlaygroundLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "QR generation failed ({ErrorType}); payload content was not logged.")]
    internal static partial void GenerationFailed(ILogger logger, string errorType);
}
