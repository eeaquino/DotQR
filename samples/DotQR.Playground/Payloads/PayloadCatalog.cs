namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    public static IReadOnlyList<PayloadDefinition> All { get; } = Build();

    private static IReadOnlyList<PayloadDefinition> Build()
    {
        List<PayloadDefinition> definitions =
        [
            new("text", "Plain text", "General", [Text("text", "Content", "https://example.com", true, FieldKind.Multiline)], _ => null)
        ];
        definitions.AddRange(Communication());
        definitions.AddRange(Contacts());
        definitions.AddRange(Crypto());
        definitions.AddRange(Payments());
        definitions.Add(SwissPayment());
        definitions.Add(RussianPayment());
        definitions.AddRange(Networks());
        return definitions.AsReadOnly();
    }

    public static PayloadValues CreateExample(string id)
    {
        var definition = All.Single(p => p.Id == id);
        var values = new PayloadValues();
        foreach (var field in definition.Fields) values[field.Key] = field.Example ?? "";
        return values;
    }

    private static PayloadField Text(string key, string label, string? example = null, bool required = false, FieldKind kind = FieldKind.Text,
        Func<PayloadValues, bool>? visible = null, string? help = null) => new(key, label, kind, example, required, Help: help, VisibleWhen: visible);
    private static PayloadField Flag(string key, string label, bool example = false, Func<PayloadValues, bool>? visible = null) =>
        new(key, label, FieldKind.Boolean, example ? "true" : "false", VisibleWhen: visible);
    private static PayloadField Choice<T>(string key, string label, T example, Func<PayloadValues, bool>? visible = null) where T : struct, Enum =>
        new(key, label, FieldKind.Select, example.ToString(), Choices: Enum.GetNames<T>(), VisibleWhen: visible);
}
