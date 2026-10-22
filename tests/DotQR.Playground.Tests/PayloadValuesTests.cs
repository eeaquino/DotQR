using DotQR.Playground.Payloads;

namespace DotQR.Playground.Tests;

public sealed class PayloadValuesTests
{
    [Fact]
    public void BlankOptionalFieldsRemainAbsent()
    {
        var values = new PayloadValues { ["blank"] = " " };
        Assert.Null(values.OptionalText("blank"));
        Assert.Null(values.OptionalInteger("blank"));
        Assert.Null(values.OptionalDecimal("blank"));
        Assert.Null(values.OptionalDate("blank"));
    }

    [Fact]
    public void InvalidNumbersNameTheirField()
    {
        var values = new PayloadValues { ["amount"] = "invalid" };
        Assert.Contains("amount", Assert.Throws<ArgumentException>(() => values.FractionalNumber("amount")).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NumbersAreInvariantAndClonesAreIndependent()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var values = new PayloadValues { ["amount"] = "1.25", ["flag"] = "false" };
            Assert.Equal(1.25m, values.FractionalNumber("amount"));
            Assert.False(values.Boolean("flag"));
            var copy = values.Clone();
            copy["amount"] = "2";
            Assert.Equal(1.25m, values.FractionalNumber("amount"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
