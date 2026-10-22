using S = DotQR.PayloadGenerator.SwissQrCode;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static PayloadDefinition SwissPayment()
    {
        List<PayloadField> fields =
        [
            Text("iban", "IBAN", "CH2609000000857666015", true), Choice("ibanType", "IBAN type", S.Iban.IbanType.Iban), Choice("currency", "Currency", S.Currency.CHF),
            Text("amount", "Amount", "12.50", kind: FieldKind.FractionalNumber), Choice("referenceType", "Reference type", S.Reference.ReferenceType.NON),
            Text("reference", "Reference", "RF18539007547034", visible: v => v.Text("referenceType") != "NON"),
            Choice("referenceTextType", "Reference text type", S.Reference.ReferenceTextType.CreditorReferenceIso11649, v => v.Text("referenceType") != "NON"),
            Text("message", "Unstructured message", "Example only"), Text("billInformation", "Bill information"),
            Text("alternative1", "Alternative procedure 1"), Text("alternative2", "Alternative procedure 2"),
            Flag("debitor", "Include debitor")
        ];
        fields.AddRange(SwissContactFields("creditor", "Creditor"));
        fields.AddRange(SwissContactFields("debitor", "Debitor", v => v.Boolean("debitor")));
        return new("SwissQrCode", "Swiss QR bill", "Payments", fields.AsReadOnly(), v => new S(
            new S.Iban(v.Text("iban"), v.Enum<S.Iban.IbanType>("ibanType")), v.Enum<S.Currency>("currency"), SwissContact(v, "creditor"),
            v.Text("referenceType") == "NON" ? new S.Reference(S.Reference.ReferenceType.NON) : new S.Reference(v.Enum<S.Reference.ReferenceType>("referenceType"), v.OptionalText("reference"), v.Enum<S.Reference.ReferenceTextType>("referenceTextType")),
            new S.AdditionalInformation(v.OptionalText("message"), v.OptionalText("billInformation")), v.Boolean("debitor") ? SwissContact(v, "debitor") : null,
            v.OptionalDecimal("amount"), null, null, v.OptionalText("alternative1"), v.OptionalText("alternative2")));
    }

    private static IEnumerable<PayloadField> SwissContactFields(string prefix, string label, Func<PayloadValues, bool>? enabled = null)
    {
        bool Visible(PayloadValues v) => enabled?.Invoke(v) != false;
        bool Structured(PayloadValues v) => Visible(v) && v.Text(prefix + "Mode") == "Structured";
        bool Combined(PayloadValues v) => Visible(v) && v.Text(prefix + "Mode") == "Combined";
        yield return new(prefix + "Mode", label + " address mode", FieldKind.Select, "Structured", Choices: ["Structured", "Combined"], VisibleWhen: Visible);
        yield return Text(prefix + "Name", label + " name", "Alex Sample", true, visible: Visible);
        yield return Text(prefix + "Country", label + " country (ISO code)", "CH", true, visible: Visible);
        yield return Text(prefix + "Zip", label + " postal code", "8000", true, visible: Structured);
        yield return Text(prefix + "City", label + " city", "Zurich", true, visible: Structured);
        yield return Text(prefix + "Street", label + " street", "Main Street", visible: Structured);
        yield return Text(prefix + "House", label + " house number", "1", visible: Structured);
        yield return Text(prefix + "Line1", label + " address line 1", "Main Street 1", true, visible: Combined);
        yield return Text(prefix + "Line2", label + " address line 2", "8000 Zurich", true, visible: Combined);
    }

    private static S.Contact SwissContact(PayloadValues v, string prefix) => v.Text(prefix + "Mode") == "Combined"
        ? S.Contact.WithCombinedAddress(v.Text(prefix + "Name"), v.Text(prefix + "Country"), v.Text(prefix + "Line1"), v.Text(prefix + "Line2"))
        : S.Contact.WithStructuredAddress(v.Text(prefix + "Name"), v.Text(prefix + "Zip"), v.Text(prefix + "City"), v.Text(prefix + "Country"), v.OptionalText(prefix + "Street"), v.OptionalText(prefix + "House"));
}
