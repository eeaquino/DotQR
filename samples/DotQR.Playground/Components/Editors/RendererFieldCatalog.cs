using DotQR.Playground.Generation;
using DotQR.Playground.Payloads;

namespace DotQR.Playground.Components.Editors;

internal static class RendererFieldCatalog
{
    internal static IReadOnlyList<PayloadField> Fields(RendererOptions o, bool micro)
    {
        List<PayloadField> fields =
        [
            new("Kind", "Output renderer", FieldKind.Select, Choices: Enum.GetNames<RendererKind>().Where(n => !micro || n != nameof(RendererKind.Art)).ToArray())
        ];
        if (o.Kind == RendererKind.Base64)
            fields.Add(Select<Base64QRCode.ImageType>("ImageType", "Encoded image format"));
        if (o.Kind == RendererKind.Svg)
            fields.Add(Select<SvgQRCode.SizingMode>("SizingMode", "SVG sizing mode"));
        if (o.Kind == RendererKind.Postscript || (o.Kind == RendererKind.Svg && o.SizingMode == SvgQRCode.SizingMode.WidthHeightAttribute))
        {
            fields.Add(Flag("ExplicitSize", "Use explicit vector dimensions"));
            if (o.ExplicitSize)
            { fields.Add(Number("Width", "Width (pixels / points)")); fields.Add(Number("Height", "Height (pixels / points)")); }
        }
        if (o.HasScale)
            fields.Add(Number("PixelsPerModule", "Pixels / points per module (1–32)"));
        if (o.HasColors)
        {
            fields.Add(new("DarkColor", "Dark modules (#RRGGBB)", FieldKind.Text));
            fields.Add(new("LightColor", "Light modules (#RRGGBB)", FieldKind.Text));
            if (o.HasAlpha)
            { fields.Add(Number("DarkAlpha", "Dark opacity (0–255)")); fields.Add(Number("LightAlpha", "Light opacity (0–255)")); }
        }
        if (o.HasQuietZones)
            fields.Add(Flag("DrawQuietZones", "Draw quiet zones"));
        if (o.Kind == RendererKind.Pdf)
            fields.Add(Number("Dpi", "PDF DPI (36–1200)"));
        if (o.Kind == RendererKind.Postscript)
            fields.Add(Flag("EpsFormat", "Encapsulated PostScript (EPS)"));
        if (o.Kind == RendererKind.Ascii)
        {
            fields.Add(Flag("Compact", "Compact half-block output"));
            if (o.Compact)
                fields.Add(Flag("Invert", "Invert compact output"));
            else
            {
                fields.Add(Number("RepeatPerModule", "Repetitions per module (1–8)"));
                fields.Add(new("DarkColorString", "Dark module string (1–8 chars)", FieldKind.Text));
                fields.Add(new("WhiteSpaceString", "Light module string (same length)", FieldKind.Text));
            }
            fields.Add(new("EndOfLine", "Line endings", FieldKind.Select, Choices: ["LF", "CRLF"]));
        }
        if (o.HasLogo && o.Logo is not null)
        {
            fields.Add(Number("IconSizePercent", "Logo coverage (1–40%)"));
            if (o.Kind == RendererKind.Svg)
                fields.Add(Flag("FillLogoBackground", "Clear modules behind logo"));
            else
            {
                fields.Add(Number("IconBorderWidth", "Logo border width (0–20)"));
                if (o.Kind == RendererKind.Skia && o.IconBorderWidth > 0)
                    fields.Add(new("IconBackgroundColor", "Logo border background (#RRGGBB)", FieldKind.Text));
            }
        }
        if (o.Kind == RendererKind.Art)
        {
            fields.Add(new("BackgroundColor", "Art background (#RRGGBB)", FieldKind.Text));
            fields.Add(new("PixelSizeFactor", "Dot size factor (0–1)", FieldKind.FractionalNumber));
            if (o.DrawQuietZones)
                fields.Add(Select<ArtQRCode.QuietZoneStyle>("QuietZoneRenderingStyle", "Quiet-zone style"));
            if (o.BackgroundImage is not null)
                fields.Add(Select<ArtQRCode.BackgroundImageStyle>("BackgroundImageStyle", "Background-image style"));
        }
        return fields;
    }

    internal static PayloadValues ToValues(RendererOptions o) => new()
    {
        ["Kind"] = o.Kind.ToString(),
        ["PixelsPerModule"] = S(o.PixelsPerModule),
        ["DarkColor"] = o.DarkColor,
        ["LightColor"] = o.LightColor,
        ["DarkAlpha"] = S(o.DarkAlpha),
        ["LightAlpha"] = S(o.LightAlpha),
        ["DrawQuietZones"] = B(o.DrawQuietZones),
        ["ExplicitSize"] = B(o.ExplicitSize),
        ["Width"] = S(o.Width),
        ["Height"] = S(o.Height),
        ["SizingMode"] = o.SizingMode.ToString(),
        ["Dpi"] = S(o.Dpi),
        ["EpsFormat"] = B(o.EpsFormat),
        ["Compact"] = B(o.Compact),
        ["Invert"] = B(o.Invert),
        ["RepeatPerModule"] = S(o.RepeatPerModule),
        ["DarkColorString"] = o.DarkColorString,
        ["WhiteSpaceString"] = o.WhiteSpaceString,
        ["EndOfLine"] = o.EndOfLine == "\r\n" ? "CRLF" : "LF",
        ["ImageType"] = o.ImageType.ToString(),
        ["IconSizePercent"] = S(o.IconSizePercent),
        ["IconBorderWidth"] = S(o.IconBorderWidth),
        ["IconBackgroundColor"] = o.IconBackgroundColor,
        ["FillLogoBackground"] = B(o.FillLogoBackground),
        ["BackgroundColor"] = o.BackgroundColor,
        ["PixelSizeFactor"] = o.PixelSizeFactor.ToString(CultureInfo.InvariantCulture),
        ["QuietZoneRenderingStyle"] = o.QuietZoneRenderingStyle.ToString(),
        ["BackgroundImageStyle"] = o.BackgroundImageStyle.ToString()
    };

    internal static RendererOptions Parse(PayloadValues v, RendererOptions previous, bool micro)
    {
        var o = previous with
        {
            Kind = v.Enum<RendererKind>("Kind"),
            ExplicitSize = v.Boolean("ExplicitSize"),
            Compact = v.Boolean("Compact"),
            ImageType = v.Enum<Base64QRCode.ImageType>("ImageType"),
            SizingMode = v.Enum<SvgQRCode.SizingMode>("SizingMode"),
            DrawQuietZones = v.Boolean("DrawQuietZones"),
            IconBorderWidth = int.TryParse(v.Text("IconBorderWidth"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var border) ? border : previous.IconBorderWidth
        };
        foreach (var field in Fields(o, micro))
            o = Apply(o, field.Key, v);
        if (o.HasColors)
        { ValidateColor(o.DarkColor); ValidateColor(o.LightColor); }
        if (o.Kind == RendererKind.Art)
            ValidateColor(o.BackgroundColor);
        if (o.Kind == RendererKind.Skia && o.Logo is not null && o.IconBorderWidth > 0)
            ValidateColor(o.IconBackgroundColor);
        return o;
    }

    private static RendererOptions Apply(RendererOptions o, string key, PayloadValues v) => key switch
    {
        "PixelsPerModule" => o with { PixelsPerModule = v.WholeNumber(key) },
        "DarkColor" => o with { DarkColor = v.Text(key) },
        "LightColor" => o with { LightColor = v.Text(key) },
        "DarkAlpha" => o with { DarkAlpha = Opacity(v.WholeNumber(key)) },
        "LightAlpha" => o with { LightAlpha = Opacity(v.WholeNumber(key)) },
        "Width" => o with { Width = v.WholeNumber(key) },
        "Height" => o with { Height = v.WholeNumber(key) },
        "Dpi" => o with { Dpi = v.WholeNumber(key) },
        "EpsFormat" => o with { EpsFormat = v.Boolean(key) },
        "Invert" => o with { Invert = v.Boolean(key) },
        "RepeatPerModule" => o with { RepeatPerModule = v.WholeNumber(key) },
        "DarkColorString" => o with { DarkColorString = v.Text(key) },
        "WhiteSpaceString" => o with { WhiteSpaceString = v.Text(key) },
        "EndOfLine" => o with { EndOfLine = v.Text(key) == "CRLF" ? "\r\n" : "\n" },
        "IconSizePercent" => o with { IconSizePercent = v.WholeNumber(key) },
        "IconBorderWidth" => o with { IconBorderWidth = v.WholeNumber(key) },
        "IconBackgroundColor" => o with { IconBackgroundColor = v.Text(key) },
        "FillLogoBackground" => o with { FillLogoBackground = v.Boolean(key) },
        "BackgroundColor" => o with { BackgroundColor = v.Text(key) },
        "PixelSizeFactor" => o with { PixelSizeFactor = v.Number(key) },
        "QuietZoneRenderingStyle" => o with { QuietZoneRenderingStyle = v.Enum<ArtQRCode.QuietZoneStyle>(key) },
        "BackgroundImageStyle" => o with { BackgroundImageStyle = v.Enum<ArtQRCode.BackgroundImageStyle>(key) },
        _ => o
    };
    private static PayloadField Number(string key, string label) => new(key, label, FieldKind.WholeNumber);
    private static PayloadField Flag(string key, string label) => new(key, label, FieldKind.Boolean);
    private static PayloadField Select<T>(string key, string label) where T : struct, Enum => new(key, label, FieldKind.Select, Choices: Enum.GetNames<T>());
    private static string S(int n) => n.ToString(CultureInfo.InvariantCulture);
    private static string B(bool b) => b ? "true" : "false";
    private static byte Opacity(int n) => n is >= 0 and <= 255 ? (byte)n : throw new ArgumentException("Opacity must be 0–255.");
    private static void ValidateColor(string color)
    {
        if (!Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
            throw new ArgumentException("Colors must use #RRGGBB (six hexadecimal digits). Use the separate opacity controls for transparency.");
    }
}
