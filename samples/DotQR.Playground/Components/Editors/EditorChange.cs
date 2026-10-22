namespace DotQR.Playground.Components.Editors;

public sealed record FieldEdit(string Key, string Value, bool Immediate);
public sealed record ImageEdit(byte[]? Bytes, string? Error, bool Busy);
public sealed record OptionsChange<T>(T? Options, string? Error, bool Immediate) where T : class;
