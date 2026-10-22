using P = DotQR.PayloadGenerator;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static IEnumerable<PayloadDefinition> Communication()
    {
        yield return new("Url", "Website URL", "General", [Text("url", "URL", "https://example.com", true)], v => new P.Url(v.Text("url")));
        yield return new("Bookmark", "Bookmark", "General", [Text("url", "URL", "https://example.com", true), Text("title", "Title", "DotQR demo", true)],
            v => new P.Bookmark(v.Text("url"), v.Text("title")));
        yield return new("PhoneNumber", "Phone number", "Communication", [Text("number", "Phone number", "+12025550123", true)], v => new P.PhoneNumber(v.Text("number")));
        yield return new("SkypeCall", "Skype call", "Communication", [Text("username", "Skype username", "demo.user", true)], v => new P.SkypeCall(v.Text("username")));
        yield return new("SMS", "SMS", "Communication", [Text("number", "Phone number", "+12025550123", true), Text("subject", "Message", "Hello from DotQR"), Choice("encoding", "Encoding", P.SMS.SMSEncoding.SMS)],
            v => new P.SMS(v.Text("number"), v.Text("subject"), v.Enum<P.SMS.SMSEncoding>("encoding")));
        yield return new("MMS", "MMS", "Communication", [Text("number", "Phone number", "+12025550123", true), Text("subject", "Subject", "Hello from DotQR"), Choice("encoding", "Encoding", P.MMS.MMSEncoding.MMS)],
            v => new P.MMS(v.Text("number"), v.Text("subject"), v.Enum<P.MMS.MMSEncoding>("encoding")));
        yield return new("Mail", "Email", "Communication", [Text("receiver", "Recipient", "demo@example.com"), Text("subject", "Subject", "Hello"), Text("message", "Message", "Hello from DotQR", kind: FieldKind.Multiline), Choice("encoding", "Encoding", P.Mail.MailEncoding.MAILTO)],
            v => new P.Mail(v.OptionalText("receiver"), v.OptionalText("subject"), v.OptionalText("message"), v.Enum<P.Mail.MailEncoding>("encoding")));
        yield return new("WhatsAppMessage", "WhatsApp", "Communication", [Flag("recipient", "Send to a specific number", true), Text("number", "Phone number", "12025550123", true, visible: v => v.Boolean("recipient")), Text("message", "Message", "Hello from DotQR", true, FieldKind.Multiline)],
            v => v.Boolean("recipient") ? new P.WhatsAppMessage(v.Text("number"), v.Text("message")) : new P.WhatsAppMessage(v.Text("message")));
        yield return new("Geolocation", "Location", "General", [Text("latitude", "Latitude", "47.3769", true), Text("longitude", "Longitude", "8.5417", true), Choice("encoding", "Encoding", P.Geolocation.GeolocationEncoding.GEO)],
            v => new P.Geolocation(v.Text("latitude"), v.Text("longitude"), v.Enum<P.Geolocation.GeolocationEncoding>("encoding")));
    }
}
