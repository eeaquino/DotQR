using R = DotQR.PayloadGenerator.RussiaPaymentOrder;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static PayloadDefinition RussianPayment()
    {
        List<PayloadField> fields =
        [
            Text("name", "Payee", "ООО Три кита", true), Text("account", "Beneficiary account", "40702810138250123017", true), Text("bank", "Bank name", "БАНК", true),
            Text("bic", "BIC", "044525225", true), Text("corresp", "Correspondent account", "30101810965770000413", true), Choice("charset", "Character set", R.CharacterSets.utf_8)
        ];
        string[] optionalText = ["Sum", "Purpose", "PayeeINN", "PayerINN", "DrawerStatus", "KPP", "CBC", "OKTMO", "PaytReason", "TaxPeriod", "DocNo", "TaxPaytKind", "LastName", "FirstName", "MiddleName", "PayerAddress", "PersonalAccount", "DocIdx", "PensAcc", "Contract", "PersAcc", "Flat", "Phone", "PayerIdType", "PayerIdNum", "ChildFio", "PaymTerm", "PaymPeriod", "Category", "ServiceName", "CounterId", "CounterVal", "QuittId", "InstNum", "ClassNum", "SpecFio", "AddAmount", "RuleId", "ExecId", "RegType", "UIN"];
        fields.AddRange(optionalText.Select(key => Text(key, key == "Sum" ? "Sum (kopecks)" : key)));
        fields.Add(Text("DocDate", "Document date", kind: FieldKind.Date));
        fields.Add(Text("BirthDate", "Birth date", kind: FieldKind.Date));
        fields.Add(Text("QuittDate", "Invoice date", kind: FieldKind.Date));
        fields.Add(new("TechCode", "Technical code (optional)", FieldKind.Select, "", Choices: ["", .. Enum.GetNames<R.TechCode>()]));
        return new("RussiaPaymentOrder", "Russian payment order", "Payments", fields.AsReadOnly(), v => new R(v.Text("name"), v.Text("account"), v.Text("bank"), v.Text("bic"), v.Text("corresp"), new R.OptionalFields
        {
            Sum = v.OptionalText("Sum"),
            Purpose = v.OptionalText("Purpose"),
            PayeeINN = v.OptionalText("PayeeINN"),
            PayerINN = v.OptionalText("PayerINN"),
            DrawerStatus = v.OptionalText("DrawerStatus"),
            KPP = v.OptionalText("KPP"),
            CBC = v.OptionalText("CBC"),
            OKTMO = v.OptionalText("OKTMO"),
            PaytReason = v.OptionalText("PaytReason"),
            TaxPeriod = v.OptionalText("TaxPeriod"),
            DocNo = v.OptionalText("DocNo"),
            DocDate = v.OptionalDate("DocDate"),
            TaxPaytKind = v.OptionalText("TaxPaytKind"),
            LastName = v.OptionalText("LastName"),
            FirstName = v.OptionalText("FirstName"),
            MiddleName = v.OptionalText("MiddleName"),
            PayerAddress = v.OptionalText("PayerAddress"),
            PersonalAccount = v.OptionalText("PersonalAccount"),
            DocIdx = v.OptionalText("DocIdx"),
            PensAcc = v.OptionalText("PensAcc"),
            Contract = v.OptionalText("Contract"),
            PersAcc = v.OptionalText("PersAcc"),
            Flat = v.OptionalText("Flat"),
            Phone = v.OptionalText("Phone"),
            PayerIdType = v.OptionalText("PayerIdType"),
            PayerIdNum = v.OptionalText("PayerIdNum"),
            ChildFio = v.OptionalText("ChildFio"),
            BirthDate = v.OptionalDate("BirthDate"),
            PaymTerm = v.OptionalText("PaymTerm"),
            PaymPeriod = v.OptionalText("PaymPeriod"),
            Category = v.OptionalText("Category"),
            ServiceName = v.OptionalText("ServiceName"),
            CounterId = v.OptionalText("CounterId"),
            CounterVal = v.OptionalText("CounterVal"),
            QuittId = v.OptionalText("QuittId"),
            QuittDate = v.OptionalDate("QuittDate"),
            InstNum = v.OptionalText("InstNum"),
            ClassNum = v.OptionalText("ClassNum"),
            SpecFio = v.OptionalText("SpecFio"),
            AddAmount = v.OptionalText("AddAmount"),
            RuleId = v.OptionalText("RuleId"),
            ExecId = v.OptionalText("ExecId"),
            RegType = v.OptionalText("RegType"),
            UIN = v.OptionalText("UIN"),
            TechCode = v.OptionalText("TechCode") is null ? null : v.Enum<R.TechCode>("TechCode")
        }, v.Enum<R.CharacterSets>("charset")));
    }
}
