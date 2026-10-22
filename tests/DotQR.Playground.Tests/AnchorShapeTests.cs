using DotQR.Playground.Components.Editors;
using DotQR.Playground.Generation;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace DotQR.Playground.Tests;

public sealed class AnchorShapeTests
{
    // Catches a missing selector, lost field parsing, or a renderer that ignores the selection.
    [Theory]
    [InlineData("Square", true)]
    [InlineData("Circle", false)]
    [InlineData("RoundedSquare", false)]
    public void SelectedAnchorShapeReachesGeneratedImage(string selection, bool squareCorner)
    {
        var options = new RendererOptions { Kind = RendererKind.Art, PixelsPerModule = 20, DarkColor = "#000000" };
        var field = Assert.Single(RendererFieldCatalog.Fields(options, false), f => f.Key == "FinderPatternShape");
        Assert.Contains(selection, field.Choices!);
        var values = RendererFieldCatalog.ToValues(options);
        values["FinderPatternShape"] = selection;
        options = RendererFieldCatalog.Parse(values, options, false);
        Assert.Equal(selection, RendererFieldCatalog.ToValues(options)["FinderPatternShape"]);
        var service = new QrGenerationService(NullLogger<QrGenerationService>.Instance);
        var outcome = service.Generate(QrGenerationServiceTests.Request() with { Renderer = options });
        Assert.Null(outcome.Error);
        using var image = SKBitmap.Decode(outcome.Result!.Content);
        Assert.Equal(squareCorner ? SKColors.Black : SKColors.White, image.GetPixel(81, 81));
    }

    [Fact]
    public void OtherRenderersDoNotOfferAnchorShape()
    {
        Assert.DoesNotContain(RendererFieldCatalog.Fields(new() { Kind = RendererKind.Png }, false),
            f => f.Key == "FinderPatternShape");
    }
}
