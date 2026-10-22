using P = DotQR.PayloadGenerator;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static IEnumerable<PayloadDefinition> Crypto()
    {
        PayloadField[] fields = [Text("address", "Address", "1BoatSLRHtKNngkdXEeobR76b53LETtpyT", true), Text("amount", "Amount", "0.01", kind: FieldKind.FractionalNumber), Text("label", "Label", "Demo"), Text("message", "Message", "Example only")];
        yield return new("BitcoinAddress", "Bitcoin", "Crypto", fields, v => new P.BitcoinAddress(v.Text("address"), v.OptionalNumber("amount"), v.OptionalText("label"), v.OptionalText("message")));
        yield return new("BitcoinCashAddress", "Bitcoin Cash", "Crypto", fields, v => new P.BitcoinCashAddress(v.Text("address"), v.OptionalNumber("amount"), v.OptionalText("label"), v.OptionalText("message")));
        yield return new("LitecoinAddress", "Litecoin", "Crypto", fields, v => new P.LitecoinAddress(v.Text("address"), v.OptionalNumber("amount"), v.OptionalText("label"), v.OptionalText("message")));
        yield return new("BitcoinLikeCryptoCurrencyAddress", "Bitcoin-like currency", "Crypto", [.. fields, Choice("currency", "Currency", P.BitcoinLikeCryptoCurrencyAddress.BitcoinLikeCryptoCurrencyType.Bitcoin)],
            v => new P.BitcoinLikeCryptoCurrencyAddress(v.Enum<P.BitcoinLikeCryptoCurrencyAddress.BitcoinLikeCryptoCurrencyType>("currency"), v.Text("address"), v.OptionalNumber("amount"), v.OptionalText("label"), v.OptionalText("message")));
        yield return new("MoneroTransaction", "Monero", "Crypto", [Text("address", "Address", "44AFFq5kSiGBoZ", true, help: "Replace with your receiving address; this example is illustrative only."), Text("amount", "Amount", "0.01", kind: FieldKind.FractionalNumber), Text("paymentId", "Payment ID"), Text("recipient", "Recipient", "Alex"), Text("description", "Description", "Example only")],
            v => new P.MoneroTransaction(v.Text("address"), (float?)v.OptionalNumber("amount"), v.OptionalText("paymentId"), v.OptionalText("recipient"), v.OptionalText("description")));
    }
}
