namespace DotQR;

/// <summary>
/// Represents a 32-bit RGBA color using 8 bits per channel.
/// </summary>
public readonly struct RgbaColor : IEquatable<RgbaColor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaColor"/> struct with a fully opaque alpha channel.
    /// </summary>
    /// <param name="red">The red channel.</param>
    /// <param name="green">The green channel.</param>
    /// <param name="blue">The blue channel.</param>
    public RgbaColor(byte red, byte green, byte blue)
        : this(red, green, blue, 255)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaColor"/> struct.
    /// </summary>
    /// <param name="red">The red channel.</param>
    /// <param name="green">The green channel.</param>
    /// <param name="blue">The blue channel.</param>
    /// <param name="alpha">The alpha channel.</param>
    public RgbaColor(byte red, byte green, byte blue, byte alpha)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;
    }

    /// <summary>Gets the red channel.</summary>
    public byte R { get; }

    /// <summary>Gets the green channel.</summary>
    public byte G { get; }

    /// <summary>Gets the blue channel.</summary>
    public byte B { get; }

    /// <summary>Gets the alpha channel.</summary>
    public byte A { get; }

    /// <summary>Gets fully opaque black.</summary>
    public static RgbaColor Black => new(0, 0, 0, 255);

    /// <summary>Gets fully opaque white.</summary>
    public static RgbaColor White => new(255, 255, 255, 255);

    /// <summary>Gets fully transparent black.</summary>
    public static RgbaColor Transparent => new(0, 0, 0, 0);

    /// <summary>Gets a value indicating whether this color is fully opaque.</summary>
    public bool IsOpaque => A == 255;

    /// <summary>Gets a value indicating whether this color is fully transparent.</summary>
    public bool IsTransparent => A == 0;

    /// <summary>
    /// Parses a color from a hex string or an HTML/CSS color name.
    /// </summary>
    /// <remarks>
    /// Accepted hex forms are <c>#RGB</c>, <c>#RRGGBB</c> and <c>#RRGGBBAA</c>, each with or
    /// without the leading <c>#</c>. Named colors are matched case-insensitively against the CSS
    /// Color Level 4 keyword set, plus <c>transparent</c>.
    /// </remarks>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException"><paramref name="value"/> is not a recognized color.</exception>
    public static RgbaColor Parse(string value)
        => TryParse(value, out var color)
            ? color
            : throw new FormatException($"'{value}' is not a valid color.");

    /// <summary>
    /// Attempts to parse a color from a hex string or an HTML/CSS color name.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="color">Receives the parsed color when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if the value was parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out RgbaColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value!.Trim();

        if (text.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            color = Transparent;
            return true;
        }

        if (_namedColors.TryGetValue(text, out color))
            return true;

        var hex = text[0] == '#' ? text.Substring(1) : text;

        if (hex.Length == 3)
        {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        if (hex.Length != 6 && hex.Length != 8)
            return false;

        Span<byte> channels = stackalloc byte[4];
        for (var i = 0; i < hex.Length / 2; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out channels[i]))
                return false;
        }

        color = hex.Length == 8
            ? new RgbaColor(channels[0], channels[1], channels[2], channels[3])
            : new RgbaColor(channels[0], channels[1], channels[2]);
        return true;
    }

    /// <summary>Returns the color as three RGB bytes.</summary>
    /// <returns>A three-element array of red, green and blue.</returns>
    public byte[] ToRgbBytes() => new[] { R, G, B };

    /// <summary>Returns the color as four RGBA bytes.</summary>
    /// <returns>A four-element array of red, green, blue and alpha.</returns>
    public byte[] ToRgbaBytes() => new[] { R, G, B, A };

    /// <summary>
    /// Returns the color as an HTML hex string.
    /// </summary>
    /// <remarks>
    /// Black and white use the <c>#000</c> and <c>#FFF</c> shorthands, a fully transparent color
    /// returns <c>transparent</c>, an opaque color returns <c>#RRGGBB</c>, and any other color
    /// returns <c>#RRGGBBAA</c>.
    /// </remarks>
    /// <returns>The HTML hex representation.</returns>
    public string ToHtmlHex()
    {
        if (this == Black)
            return "#000";
        if (this == White)
            return "#FFF";
        if (A == 0)
            return "transparent";
        if (A == 255)
            return $"#{R:X2}{G:X2}{B:X2}";
        return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }

    /// <inheritdoc/>
    public bool Equals(RgbaColor other)
        => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RgbaColor other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <inheritdoc/>
    public override string ToString() => ToHtmlHex();

    /// <summary>Compares two colors for equality.</summary>
    /// <param name="left">The left color.</param>
    /// <param name="right">The right color.</param>
    /// <returns><see langword="true"/> if the colors are identical.</returns>
    public static bool operator ==(RgbaColor left, RgbaColor right) => left.Equals(right);

    /// <summary>Compares two colors for inequality.</summary>
    /// <param name="left">The left color.</param>
    /// <param name="right">The right color.</param>
    /// <returns><see langword="true"/> if the colors differ.</returns>
    public static bool operator !=(RgbaColor left, RgbaColor right) => !left.Equals(right);

    private static RgbaColor Hex(uint rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    /// <summary>
    /// CSS Color Level 4 named colors. Keys are lower case so lookups can ignore case.
    /// </summary>
    private static readonly Dictionary<string, RgbaColor> _namedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aliceblue"] = Hex(0xF0F8FF),
        ["antiquewhite"] = Hex(0xFAEBD7),
        ["aqua"] = Hex(0x00FFFF),
        ["aquamarine"] = Hex(0x7FFFD4),
        ["azure"] = Hex(0xF0FFFF),
        ["beige"] = Hex(0xF5F5DC),
        ["bisque"] = Hex(0xFFE4C4),
        ["black"] = Black,
        ["blanchedalmond"] = Hex(0xFFEBCD),
        ["blue"] = Hex(0x0000FF),
        ["blueviolet"] = Hex(0x8A2BE2),
        ["brown"] = Hex(0xA52A2A),
        ["burlywood"] = Hex(0xDEB887),
        ["cadetblue"] = Hex(0x5F9EA0),
        ["chartreuse"] = Hex(0x7FFF00),
        ["chocolate"] = Hex(0xD2691E),
        ["coral"] = Hex(0xFF7F50),
        ["cornflowerblue"] = Hex(0x6495ED),
        ["cornsilk"] = Hex(0xFFF8DC),
        ["crimson"] = Hex(0xDC143C),
        ["cyan"] = Hex(0x00FFFF),
        ["darkblue"] = Hex(0x00008B),
        ["darkcyan"] = Hex(0x008B8B),
        ["darkgoldenrod"] = Hex(0xB8860B),
        ["darkgray"] = Hex(0xA9A9A9),
        ["darkgreen"] = Hex(0x006400),
        ["darkgrey"] = Hex(0xA9A9A9),
        ["darkkhaki"] = Hex(0xBDB76B),
        ["darkmagenta"] = Hex(0x8B008B),
        ["darkolivegreen"] = Hex(0x556B2F),
        ["darkorange"] = Hex(0xFF8C00),
        ["darkorchid"] = Hex(0x9932CC),
        ["darkred"] = Hex(0x8B0000),
        ["darksalmon"] = Hex(0xE9967A),
        ["darkseagreen"] = Hex(0x8FBC8F),
        ["darkslateblue"] = Hex(0x483D8B),
        ["darkslategray"] = Hex(0x2F4F4F),
        ["darkslategrey"] = Hex(0x2F4F4F),
        ["darkturquoise"] = Hex(0x00CED1),
        ["darkviolet"] = Hex(0x9400D3),
        ["deeppink"] = Hex(0xFF1493),
        ["deepskyblue"] = Hex(0x00BFFF),
        ["dimgray"] = Hex(0x696969),
        ["dimgrey"] = Hex(0x696969),
        ["dodgerblue"] = Hex(0x1E90FF),
        ["firebrick"] = Hex(0xB22222),
        ["floralwhite"] = Hex(0xFFFAF0),
        ["forestgreen"] = Hex(0x228B22),
        ["fuchsia"] = Hex(0xFF00FF),
        ["gainsboro"] = Hex(0xDCDCDC),
        ["ghostwhite"] = Hex(0xF8F8FF),
        ["gold"] = Hex(0xFFD700),
        ["goldenrod"] = Hex(0xDAA520),
        ["gray"] = Hex(0x808080),
        ["green"] = Hex(0x008000),
        ["greenyellow"] = Hex(0xADFF2F),
        ["grey"] = Hex(0x808080),
        ["honeydew"] = Hex(0xF0FFF0),
        ["hotpink"] = Hex(0xFF69B4),
        ["indianred"] = Hex(0xCD5C5C),
        ["indigo"] = Hex(0x4B0082),
        ["ivory"] = Hex(0xFFFFF0),
        ["khaki"] = Hex(0xF0E68C),
        ["lavender"] = Hex(0xE6E6FA),
        ["lavenderblush"] = Hex(0xFFF0F5),
        ["lawngreen"] = Hex(0x7CFC00),
        ["lemonchiffon"] = Hex(0xFFFACD),
        ["lightblue"] = Hex(0xADD8E6),
        ["lightcoral"] = Hex(0xF08080),
        ["lightcyan"] = Hex(0xE0FFFF),
        ["lightgoldenrodyellow"] = Hex(0xFAFAD2),
        ["lightgray"] = Hex(0xD3D3D3),
        ["lightgreen"] = Hex(0x90EE90),
        ["lightgrey"] = Hex(0xD3D3D3),
        ["lightpink"] = Hex(0xFFB6C1),
        ["lightsalmon"] = Hex(0xFFA07A),
        ["lightseagreen"] = Hex(0x20B2AA),
        ["lightskyblue"] = Hex(0x87CEFA),
        ["lightslategray"] = Hex(0x778899),
        ["lightslategrey"] = Hex(0x778899),
        ["lightsteelblue"] = Hex(0xB0C4DE),
        ["lightyellow"] = Hex(0xFFFFE0),
        ["lime"] = Hex(0x00FF00),
        ["limegreen"] = Hex(0x32CD32),
        ["linen"] = Hex(0xFAF0E6),
        ["magenta"] = Hex(0xFF00FF),
        ["maroon"] = Hex(0x800000),
        ["mediumaquamarine"] = Hex(0x66CDAA),
        ["mediumblue"] = Hex(0x0000CD),
        ["mediumorchid"] = Hex(0xBA55D3),
        ["mediumpurple"] = Hex(0x9370DB),
        ["mediumseagreen"] = Hex(0x3CB371),
        ["mediumslateblue"] = Hex(0x7B68EE),
        ["mediumspringgreen"] = Hex(0x00FA9A),
        ["mediumturquoise"] = Hex(0x48D1CC),
        ["mediumvioletred"] = Hex(0xC71585),
        ["midnightblue"] = Hex(0x191970),
        ["mintcream"] = Hex(0xF5FFFA),
        ["mistyrose"] = Hex(0xFFE4E1),
        ["moccasin"] = Hex(0xFFE4B5),
        ["navajowhite"] = Hex(0xFFDEAD),
        ["navy"] = Hex(0x000080),
        ["oldlace"] = Hex(0xFDF5E6),
        ["olive"] = Hex(0x808000),
        ["olivedrab"] = Hex(0x6B8E23),
        ["orange"] = Hex(0xFFA500),
        ["orangered"] = Hex(0xFF4500),
        ["orchid"] = Hex(0xDA70D6),
        ["palegoldenrod"] = Hex(0xEEE8AA),
        ["palegreen"] = Hex(0x98FB98),
        ["paleturquoise"] = Hex(0xAFEEEE),
        ["palevioletred"] = Hex(0xDB7093),
        ["papayawhip"] = Hex(0xFFEFD5),
        ["peachpuff"] = Hex(0xFFDAB9),
        ["peru"] = Hex(0xCD853F),
        ["pink"] = Hex(0xFFC0CB),
        ["plum"] = Hex(0xDDA0DD),
        ["powderblue"] = Hex(0xB0E0E6),
        ["purple"] = Hex(0x800080),
        ["rebeccapurple"] = Hex(0x663399),
        ["red"] = Hex(0xFF0000),
        ["rosybrown"] = Hex(0xBC8F8F),
        ["royalblue"] = Hex(0x4169E1),
        ["saddlebrown"] = Hex(0x8B4513),
        ["salmon"] = Hex(0xFA8072),
        ["sandybrown"] = Hex(0xF4A460),
        ["seagreen"] = Hex(0x2E8B57),
        ["seashell"] = Hex(0xFFF5EE),
        ["sienna"] = Hex(0xA0522D),
        ["silver"] = Hex(0xC0C0C0),
        ["skyblue"] = Hex(0x87CEEB),
        ["slateblue"] = Hex(0x6A5ACD),
        ["slategray"] = Hex(0x708090),
        ["slategrey"] = Hex(0x708090),
        ["snow"] = Hex(0xFFFAFA),
        ["springgreen"] = Hex(0x00FF7F),
        ["steelblue"] = Hex(0x4682B4),
        ["tan"] = Hex(0xD2B48C),
        ["teal"] = Hex(0x008080),
        ["thistle"] = Hex(0xD8BFD8),
        ["tomato"] = Hex(0xFF6347),
        ["turquoise"] = Hex(0x40E0D0),
        ["violet"] = Hex(0xEE82EE),
        ["wheat"] = Hex(0xF5DEB3),
        ["white"] = White,
        ["whitesmoke"] = Hex(0xF5F5F5),
        ["yellow"] = Hex(0xFFFF00),
        ["yellowgreen"] = Hex(0x9ACD32),
    };
}
