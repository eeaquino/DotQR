using P = DotQR.PayloadGenerator;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static IEnumerable<PayloadDefinition> Payments()
    {
        yield return new("Girocode", "SEPA / Girocode", "Payments",
        [
            Text("iban", "IBAN", "DE89370400440532013000", true), Text("bic", "BIC", "COBADEFFXXX"), Text("name", "Payee", "Alex Sample", true),
            Text("amount", "Amount (EUR)", "12.50", true, FieldKind.Decimal), Text("remittance", "Remittance", "Demo invoice"),
            Choice("remittanceType", "Remittance type", P.Girocode.TypeOfRemittance.Unstructured), Text("purpose", "Purpose code"), Text("message", "Message to user"),
            Choice("version", "Girocode version", P.Girocode.GirocodeVersion.Version1), Choice("encoding", "Character encoding", P.Girocode.GirocodeEncoding.UTF_8)
        ], v => new P.Girocode(v.Text("iban"), v.OptionalText("bic"), v.Text("name"), v.Decimal("amount"), v.Text("remittance"), v.Enum<P.Girocode.TypeOfRemittance>("remittanceType"), v.Text("purpose"), v.Text("message"), v.Enum<P.Girocode.GirocodeVersion>("version"), v.Enum<P.Girocode.GirocodeEncoding>("encoding")));

        yield return new("SlovenianUpnQr", "Slovenian UPN", "Payments",
        [
            Text("payerName", "Payer", "Janez Novak", true), Text("payerAddress", "Payer address", "Slovenska cesta 1", true), Text("payerPlace", "Payer place", "1000 Ljubljana", true),
            Text("recipientName", "Recipient", "Podjetje d.o.o.", true), Text("recipientAddress", "Recipient address", "Trzaska cesta 10", true), Text("recipientPlace", "Recipient place", "2000 Maribor", true),
            Text("iban", "Recipient IBAN", "SI56011006001234567", true), Text("description", "Description", "Payment for services", true), Text("amount", "Amount", "150.50", true, FieldKind.Decimal),
            Text("deadline", "Deadline", kind: FieldKind.Date), Text("model", "SI model", "SI00", true), Text("reference", "SI reference"), Text("code", "Purpose code", "OTHR", true)
        ], v => new P.SlovenianUpnQr(v.Text("payerName"), v.Text("payerAddress"), v.Text("payerPlace"), v.Text("recipientName"), v.Text("recipientAddress"), v.Text("recipientPlace"), v.Text("iban"), v.Text("description"), v.Number("amount"), v.OptionalDate("deadline"), v.Text("model"), v.Text("reference"), v.Text("code")));

        yield return new("BezahlCode", "BezahlCode", "Payments", BezahlFields(), CreateBezahl);
    }

    private static bool IsContact(PayloadValues v) => v.Text("authority") is "contact" or "contact_v2";
    private static bool IsLegacyContact(PayloadValues v) => v.Text("authority") == "contact" || (v.Text("authority") == "contact_v2" && v.Boolean("legacyAccount"));
    private static bool IsPeriodic(PayloadValues v) => v.Text("authority") == "periodicsinglepaymentsepa";
    private static bool IsDebit(PayloadValues v) => v.Text("authority") == "singledirectdebitsepa";
    private static IReadOnlyList<PayloadField> BezahlFields() =>
    [
        new("authority", "Operation", FieldKind.Select, "singlepaymentsepa", Choices: ["singlepaymentsepa", "singledirectdebitsepa", "periodicsinglepaymentsepa", "contact", "contact_v2"]),
        Text("name", "Payee", "Alex Sample", true), Flag("legacyAccount", "Use account / BNC", visible: v => v.Text("authority") == "contact_v2"),
        Text("account", "Account", "123456789", true, visible: IsLegacyContact), Text("bnc", "BNC", "12345678", true, visible: IsLegacyContact),
        Text("iban", "IBAN", "DE89370400440532013000", true, visible: v => !IsLegacyContact(v)), Text("bic", "BIC", "COBADEFFXXX", true, visible: v => !IsLegacyContact(v)),
        Text("amount", "Amount", "12.50", true, FieldKind.Decimal, v => !IsContact(v)), Choice("currency", "Currency", P.BezahlCode.Currency.EUR, v => !IsContact(v)),
        Text("reason", "Reason", "Demo invoice"), Text("executionDate", "Execution date (blank = today)", kind: FieldKind.Date, visible: v => !IsContact(v)),
        Text("sepaReference", "SEPA reference", visible: v => !IsContact(v)), Text("creditorId", "Creditor ID", visible: IsDebit), Text("mandateId", "Mandate ID", visible: IsDebit),
        Text("signatureDate", "Mandate signature date", kind: FieldKind.Date, visible: IsDebit),
        new("timeunit", "Repeat unit", FieldKind.Select, "M", Choices: ["M", "W"], VisibleWhen: IsPeriodic), Text("rotation", "Repeat every", "1", true, FieldKind.Integer, IsPeriodic),
        Text("firstDate", "First execution", kind: FieldKind.Date, visible: IsPeriodic), Text("lastDate", "Last execution", kind: FieldKind.Date, visible: IsPeriodic)
    ];

    private static P.BezahlCode CreateBezahl(PayloadValues v)
    {
        var authority = v.Enum<P.BezahlCode.AuthorityType>("authority");
        if (IsContact(v)) return new(authority, v.Text("name"), IsLegacyContact(v) ? v.Text("account") : "", IsLegacyContact(v) ? v.Text("bnc") : "", IsLegacyContact(v) ? "" : v.Text("iban"), IsLegacyContact(v) ? "" : v.Text("bic"), v.Text("reason"));
        return new(authority, v.Text("name"), v.Text("iban"), v.Text("bic"), v.Decimal("amount"),
            IsPeriodic(v) ? v.Text("timeunit") : "", IsPeriodic(v) ? v.Integer("rotation") : 0,
            IsPeriodic(v) ? v.OptionalDate("firstDate") : null, IsPeriodic(v) ? v.OptionalDate("lastDate") : null,
            IsDebit(v) ? v.Text("creditorId") : "", IsDebit(v) ? v.Text("mandateId") : "", IsDebit(v) ? v.OptionalDate("signatureDate") : null,
            v.Text("reason"), v.Text("sepaReference"), v.Enum<P.BezahlCode.Currency>("currency"), v.OptionalDate("executionDate"));
    }
}
