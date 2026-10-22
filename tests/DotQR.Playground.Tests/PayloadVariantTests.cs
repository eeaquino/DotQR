using DotQR.Playground.Payloads;

namespace DotQR.Playground.Tests;

public sealed class PayloadVariantTests
{
    [Theory]
    [InlineData("VCard21", "VERSION:2.1")]
    [InlineData("VCard3", "VERSION:3.0")]
    [InlineData("VCard4", "VERSION:4.0")]
    [InlineData("MeCard", "MECARD+")]
    public void ContactFormatSelectionChangesPayload(string format, string expected)
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "ContactData");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["outputType"] = format;
        Assert.Contains(expected, definition.Create(values)!.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("VCard21", false, true)]
    [InlineData("VCard3", true, true)]
    [InlineData("VCard4", true, true)]
    [InlineData("MeCard", true, false)]
    public void ContactOnlyOffersFieldsItsFormatSerializes(string format, bool nickname, bool addressType)
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "ContactData");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["outputType"] = format;
        Assert.Equal(nickname, definition.Fields.Single(f => f.Key == "nickname").VisibleWhen?.Invoke(values) != false);
        Assert.Equal(addressType, definition.Fields.Single(f => f.Key == "addressType").VisibleWhen?.Invoke(values) != false);
    }

    [Theory]
    [InlineData("None", "ss://")]
    [InlineData("Plugin", "plugin=obfs-local%3bobfs%3dhttp")]
    [InlineData("Parameters", "plugin=obfs-local%3bobfs%3dhttp")]
    public void ShadowsocksOptionsReachPayload(string mode, string expected)
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "ShadowSocksConfig");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["mode"] = mode;
        Assert.Contains(expected, definition.Create(values)!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SwissDebitorUsesCombinedAddressFields()
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "SwissQrCode");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["debitor"] = "true";
        values["debitorMode"] = "Combined";
        values["debitorName"] = "Unique Debitor";
        values["debitorLine1"] = "Sample Lane 2";
        values["debitorLine2"] = "1000 Lausanne";
        Assert.Contains("K\r\nUnique Debitor\r\nSample Lane 2\r\n1000 Lausanne", definition.Create(values)!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RecurringSepaPaymentMapsRecurrence()
    {
        var definition = PayloadCatalog.All.Single(p => p.Id == "BezahlCode");
        var values = PayloadCatalog.CreateExample(definition.Id);
        values["authority"] = "periodicsinglepaymentsepa";
        values["timeunit"] = "W";
        values["rotation"] = "2";
        var payload = definition.Create(values)!.ToString();
        Assert.StartsWith("bank://periodicsinglepaymentsepa?", payload, StringComparison.Ordinal);
        Assert.Contains("periodictimeunit=W", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("periodictimeunitrotation=2", payload, StringComparison.OrdinalIgnoreCase);
    }
}
