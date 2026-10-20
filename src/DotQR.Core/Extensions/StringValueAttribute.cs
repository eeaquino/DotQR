using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DotQR.Extensions;

/// <summary>
/// Used to represent a string value for a value in an enum
/// </summary>
[Obsolete("This attribute will be removed in a future version of DotQR.")]
[AttributeUsage(AttributeTargets.Field)]
public class StringValueAttribute : Attribute
{

    #region Properties

    /// <summary>
    /// Holds the alue in an enum
    /// </summary>
    public string StringValue { get; protected set; }

    #endregion

    /// <summary>
    /// Init a StringValue Attribute
    /// </summary>
    /// <param name="value"></param>
    public StringValueAttribute(string value)
    {
        StringValue = value;
    }
}

/// <summary>
/// Enumeration extension methods.
/// </summary>
[Obsolete("This class will be removed in a future version of DotQR.")]
public static class CustomExtensions
{
    /// <summary>
    /// Will get the string value for a given enum's value
    /// </summary>
    [RequiresUnreferencedCode("This method uses reflection to examine the provided enum value.")]
    public static string? GetStringValue(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString())!;
        var attr = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
        return attr!.Length > 0 ? attr[0].StringValue : null;
    }
}
