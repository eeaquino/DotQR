using P = DotQR.PayloadGenerator;

namespace DotQR.Playground.Payloads;

public static partial class PayloadCatalog
{
    private static IEnumerable<PayloadDefinition> Contacts()
    {
        yield return new("ContactData", "Contact / vCard", "People & events",
        [
            Choice("outputType", "Contact format", P.ContactData.ContactOutputType.VCard3),
            Text("firstname", "First name", "Alex", true), Text("lastname", "Last name", "Sample", true),
            Text("nickname", "Nickname"), Text("phone", "Home phone"), Text("mobilePhone", "Mobile phone", "+12025550123"), Text("workPhone", "Work phone"),
            Text("email", "Email", "alex@example.com"), Text("birthday", "Birthday", kind: FieldKind.Date), Text("website", "Website", "https://example.com"),
            Text("street", "Street", "Main Street"), Text("houseNumber", "House number", "1"), Text("city", "City", "Zurich"), Text("zipCode", "Postal code", "8000"),
            Text("country", "Country", "Switzerland"), Text("note", "Note", kind: FieldKind.Multiline), Text("stateRegion", "State / region"),
            Choice("addressOrder", "Address order", P.ContactData.AddressOrder.Default), Text("org", "Organization", "Example Ltd"), Text("orgTitle", "Job title"),
            Choice("addressType", "Address type", P.ContactData.AddressType.Home)
        ], v => new P.ContactData(v.Enum<P.ContactData.ContactOutputType>("outputType"), v.Text("firstname"), v.Text("lastname"),
            v.OptionalText("nickname"), v.OptionalText("phone"), v.OptionalText("mobilePhone"), v.OptionalText("workPhone"), v.OptionalText("email"), v.OptionalDate("birthday"),
            v.OptionalText("website"), v.OptionalText("street"), v.OptionalText("houseNumber"), v.OptionalText("city"), v.OptionalText("zipCode"), v.OptionalText("country"),
            v.OptionalText("note"), v.OptionalText("stateRegion"), v.Enum<P.ContactData.AddressOrder>("addressOrder"), v.OptionalText("org"), v.OptionalText("orgTitle"), v.Enum<P.ContactData.AddressType>("addressType")));

        yield return new("CalendarEvent", "Calendar event", "People & events",
        [
            Text("subject", "Subject", "DotQR meetup", true), Text("description", "Description", "Bring your QR scanner", kind: FieldKind.Multiline), Text("location", "Location", "Zurich"),
            Text("start", "Start (UTC)", "2026-11-01T10:00", true, FieldKind.DateTime), Text("end", "End (UTC)", "2026-11-01T11:00", true, FieldKind.DateTime),
            Flag("allDay", "All-day event"), Choice("encoding", "Calendar format", P.CalendarEvent.EventEncoding.Universal)
        ], v => new P.CalendarEvent(v.Text("subject"), v.OptionalText("description"), v.OptionalText("location"),
            DateTime.SpecifyKind(v.Date("start"), DateTimeKind.Utc), DateTime.SpecifyKind(v.Date("end"), DateTimeKind.Utc), v.Boolean("allDay"), v.Enum<P.CalendarEvent.EventEncoding>("encoding")));
    }
}
