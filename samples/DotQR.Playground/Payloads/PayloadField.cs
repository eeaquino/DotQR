namespace DotQR.Playground.Payloads;

public enum FieldKind { Text, Multiline, Password, Integer, Decimal, Boolean, Date, DateTime, Select }

public sealed record PayloadField(
    string Key, string Label, FieldKind Kind, string? Example = null,
    bool Required = false, IReadOnlyList<string>? Choices = null, string? Help = null,
    Func<PayloadValues, bool>? VisibleWhen = null);
