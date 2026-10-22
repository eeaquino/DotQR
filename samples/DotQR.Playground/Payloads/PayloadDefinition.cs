namespace DotQR.Playground.Payloads;

public sealed record PayloadDefinition(string Id, string Label, string Group,
    IReadOnlyList<PayloadField> Fields, Func<PayloadValues, PayloadGenerator.Payload?> Create);
