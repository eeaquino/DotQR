using DotQR.Playground.Payloads;

namespace DotQR.Playground.Generation;

public sealed record GenerationRequest(PayloadDefinition Definition, PayloadValues Values, EncodingOptions Encoding, RendererOptions Renderer)
{
    public GenerationRequest Snapshot() => this with { Values = Values.Clone(), Renderer = Renderer.Snapshot() };
}
