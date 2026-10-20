namespace DotQRTests.Core.Primitives;

public class RgbaColorTests
{
    [Theory]
    [InlineData("#000000", 0, 0, 0, 255)]
    [InlineData("#ffffff", 255, 255, 255, 255)]
    [InlineData("000000", 0, 0, 0, 255)]
    [InlineData("#f80", 255, 136, 0, 255)]
    [InlineData("#FF880080", 255, 136, 0, 128)]
    [InlineData("red", 255, 0, 0, 255)]
    [InlineData("White", 255, 255, 255, 255)]
    [InlineData("transparent", 0, 0, 0, 0)]
    public void Parses_hex_and_named_colors(string input, byte r, byte g, byte b, byte a)
    {
        var color = RgbaColor.Parse(input);
        color.ShouldBe(new RgbaColor(r, g, b, a));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-color")]
    [InlineData("#12")]
    [InlineData("#12345")]
    public void TryParse_returns_false_for_invalid_input(string? input)
    {
        RgbaColor.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void Parse_throws_FormatException_for_invalid_input()
    {
        Should.Throw<FormatException>(() => RgbaColor.Parse("not-a-color"));
    }

    [Fact]
    public void Black_white_and_transparent_match_expected_values()
    {
        RgbaColor.Black.ShouldBe(new RgbaColor(0, 0, 0, 255));
        RgbaColor.White.ShouldBe(new RgbaColor(255, 255, 255, 255));
        RgbaColor.Transparent.ShouldBe(new RgbaColor(0, 0, 0, 0));
    }

    [Fact]
    public void Three_argument_constructor_is_opaque()
    {
        new RgbaColor(10, 20, 30).A.ShouldBe((byte)255);
    }

    [Fact]
    public void Opaque_and_transparent_flags_are_derived_from_alpha()
    {
        new RgbaColor(1, 2, 3, 255).IsOpaque.ShouldBeTrue();
        new RgbaColor(1, 2, 3, 254).IsOpaque.ShouldBeFalse();
        new RgbaColor(1, 2, 3, 0).IsTransparent.ShouldBeTrue();
        new RgbaColor(1, 2, 3, 1).IsTransparent.ShouldBeFalse();
    }

    [Fact]
    public void ToRgbBytes_and_ToRgbaBytes_have_expected_widths()
    {
        var color = new RgbaColor(10, 20, 30, 40);
        color.ToRgbBytes().ShouldBe(new byte[] { 10, 20, 30 });
        color.ToRgbaBytes().ShouldBe(new byte[] { 10, 20, 30, 40 });
    }

    [Theory]
    [InlineData(0, 0, 0, 255, "#000")]
    [InlineData(255, 255, 255, 255, "#FFF")]
    [InlineData(0, 0, 0, 0, "transparent")]
    [InlineData(255, 136, 0, 255, "#FF8800")]
    [InlineData(255, 136, 0, 128, "#FF880080")]
    public void ToHtmlHex_reproduces_legacy_svg_shorthand_rules(byte r, byte g, byte b, byte a, string expected)
    {
        new RgbaColor(r, g, b, a).ToHtmlHex().ShouldBe(expected);
    }

    [Fact]
    public void Equality_compares_all_four_channels()
    {
        (new RgbaColor(1, 2, 3, 4) == new RgbaColor(1, 2, 3, 4)).ShouldBeTrue();
        (new RgbaColor(1, 2, 3, 4) == new RgbaColor(1, 2, 3, 5)).ShouldBeFalse();
        (new RgbaColor(1, 2, 3, 4) != new RgbaColor(1, 2, 3, 5)).ShouldBeTrue();
        new RgbaColor(1, 2, 3, 4).GetHashCode().ShouldBe(new RgbaColor(1, 2, 3, 4).GetHashCode());
    }

    [Fact]
    public void Round_trips_through_ToHtmlHex_when_opaque()
    {
        var original = new RgbaColor(17, 34, 51, 255);
        RgbaColor.Parse(original.ToHtmlHex()).ShouldBe(original);
    }
}
