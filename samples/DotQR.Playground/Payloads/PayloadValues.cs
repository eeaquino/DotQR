namespace DotQR.Playground.Payloads;

public sealed class PayloadValues
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    public string this[string key] { get => _values.GetValueOrDefault(key, ""); set => _values[key] = value; }
    public string Text(string key) => this[key];
    public string? OptionalText(string key) => string.IsNullOrWhiteSpace(this[key]) ? null : this[key];
    public int WholeNumber(string key) => int.TryParse(this[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        ? value : throw Invalid(key, "a whole number");
    public int? OptionalInteger(string key) => OptionalText(key) is null ? null : WholeNumber(key);
    public decimal FractionalNumber(string key) => decimal.TryParse(this[key], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
        ? value : throw Invalid(key, "a decimal number (use a decimal point)");
    public decimal? OptionalDecimal(string key) => OptionalText(key) is null ? null : FractionalNumber(key);
    public double Number(string key) => double.TryParse(this[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
        ? value : throw Invalid(key, "a finite number");
    public double? OptionalNumber(string key) => OptionalText(key) is null ? null : Number(key);
    public bool Boolean(string key) => bool.TryParse(this[key], out var value) && value;
    public DateTime Date(string key) => DateTime.TryParse(this[key], CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
        ? value : throw Invalid(key, "a valid date/time");
    public DateTime? OptionalDate(string key) => OptionalText(key) is null ? null : Date(key);
    public T Enum<T>(string key) where T : struct, Enum => System.Enum.TryParse<T>(this[key], out var value) && System.Enum.IsDefined(value)
        ? value : throw Invalid(key, $"a supported {typeof(T).Name}");
    public PayloadValues Clone()
    {
        var copy = new PayloadValues();
        foreach (var (key, value) in _values)
            copy[key] = value;
        return copy;
    }
    public void Validate(IReadOnlyList<PayloadField> fields)
    {
        foreach (var field in fields)
        {
            if (field.VisibleWhen?.Invoke(this) == false)
                continue;
            if (field.Required && OptionalText(field.Key) is null)
                throw Invalid(field.Label, "a value");
            if (this[field.Key].Length > 8192)
                throw Invalid(field.Label, "at most 8192 characters");
            if (field.Key.Equals("iban", StringComparison.OrdinalIgnoreCase) && OptionalText(field.Key) is { } iban
                && !Regex.IsMatch(iban.Replace(" ", "").Replace("-", ""), "^[A-Za-z]{2}[0-9]{2}[A-Za-z0-9]{11,30}$"))
                throw Invalid(field.Label, "a valid IBAN");
        }
    }
    private static ArgumentException Invalid(string key, string expected) => new($"{key}: enter {expected}.");
}
