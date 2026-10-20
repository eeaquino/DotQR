using System.Reflection;

namespace DotQRTests.ByteRenderers.Helpers;

/// <summary>
/// Embedded test assets. Graphics-free: the icon is exposed as raw PNG bytes and SVG text only.
/// </summary>
public static class HelperFunctions
{
    /// <summary>Gets the embedded PNG icon as a byte array.</summary>
    public static byte[] GetIconBytes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "DotQRTests.ByteRenderers.assets.noun_software engineer_2909346.png";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Gets the embedded SVG logo as a string.</summary>
    public static string GetIconSvg()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "DotQRTests.ByteRenderers.assets.noun_Scientist_2909361.svg";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
