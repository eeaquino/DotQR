using DotQR.Playground.Payloads;

namespace DotQR.Playground.Tests;

public sealed class PayloadCatalogTests
{
    [Fact]
    public void CatalogueIncludesEveryPayloadAndWorkingExamples()
    {
        string[] expected = ["text", "BezahlCode", "BitcoinAddress", "BitcoinCashAddress", "BitcoinLikeCryptoCurrencyAddress", "Bookmark", "CalendarEvent", "ContactData", "Geolocation", "Girocode", "LitecoinAddress", "MMS", "Mail", "MoneroTransaction", "OneTimePassword", "PhoneNumber", "RussiaPaymentOrder", "ShadowSocksConfig", "SkypeCall", "SMS", "SlovenianUpnQr", "SwissQrCode", "Url", "WhatsAppMessage", "WiFi"];
        Assert.Equal(expected.Order(StringComparer.Ordinal), PayloadCatalog.All.Select(p => p.Id).Order(StringComparer.Ordinal));
        foreach (var definition in PayloadCatalog.All)
        {
            var values = PayloadCatalog.CreateExample(definition.Id);
            var payload = definition.Create(values);
            Assert.False(string.IsNullOrEmpty(payload?.ToString() ?? values.Text("text")));
            Assert.Equal(definition.Fields.Count, definition.Fields.Select(f => f.Key).Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void ResetDoesNotReuseMutatedValues()
    {
        var values = PayloadCatalog.CreateExample("WiFi");
        values["ssid"] = "changed";
        Assert.Equal("DotQR Guest", PayloadCatalog.CreateExample("WiFi").Text("ssid"));
    }

    [Fact]
    public void WifiOptionalFlagsReachSerializer()
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "WiFi");
        var values = PayloadCatalog.CreateExample("WiFi");
        values["ssid"] = "Demo";
        values["password"] = "secret";
        values["hidden"] = "true";
        Assert.Equal("WIFI:T:WPA;S:Demo;P:secret;H:true;", definition.Create(values)!.ToString());
    }

    [Fact]
    public void HotpCounterReachesSerializer()
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "OneTimePassword");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["type"] = "HOTP";
        values["counter"] = "42";
        Assert.Contains("counter=42", definition.Create(values)!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void OptionalRussianFieldReachesSerializer()
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "RussiaPaymentOrder");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["Purpose"] = "Demo payment";
        Assert.Contains("Purpose=Demo payment", definition.Create(values)!.ToString(), StringComparison.Ordinal);
    }
}
