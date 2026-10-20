using System.Reflection;
using System.Text;
using Shouldly;

namespace DotQRTests.SkiaSharp;

/// <summary>
/// Pins the public API surface of each shipped assembly, so an accidental breaking change shows up
/// as a reviewable diff instead of shipping.
/// </summary>
public class ApiSurfaceTests
{
    [Fact]
    public void Core_public_api_is_stable()
        => BuildSurface(typeof(QRCodeGenerator).Assembly).ShouldMatchApprovedText("txt");

    [Fact]
    public void ByteRenderers_public_api_is_stable()
        => BuildSurface(typeof(PngByteQRCode).Assembly).ShouldMatchApprovedText("txt");

    [Fact]
    public void SkiaSharp_public_api_is_stable()
        => BuildSurface(typeof(DotQR.QRCode).Assembly).ShouldMatchApprovedText("txt");

    /// <summary>
    /// Produces a deterministic, reviewable listing of an assembly's public API.
    /// </summary>
    /// <param name="assembly">The assembly to describe.</param>
    /// <returns>The sorted API surface.</returns>
    private static string BuildSurface(Assembly assembly)
    {
        var builder = new StringBuilder();
        builder.Append("ASSEMBLY ").Append(assembly.GetName().Name).Append('\n');

        foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            builder.Append('\n').Append("TYPE ").Append(type.FullName);
            builder.Append(" [").Append(DescribeTypeKind(type)).Append(']');
            if (type.BaseType is { } baseType && baseType != typeof(object) && baseType != typeof(ValueType))
                builder.Append(" : ").Append(baseType.FullName);
            builder.Append('\n');

            var members = new List<string>();

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                members.Add("  CTOR (" + DescribeParameters(constructor.GetParameters()) + ")");

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var accessors = new List<string>();
                if (property.GetMethod?.IsPublic == true)
                    accessors.Add("get");
                if (property.SetMethod?.IsPublic == true)
                    accessors.Add("set");
                if (accessors.Count == 0)
                    continue;
                members.Add($"  PROPERTY {property.Name} : {DescribeType(property.PropertyType)} ({string.Join("/", accessors)})");
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                // Skip compiler-generated backing fields of auto-properties, records, and the
                // enum value__ slot.
                if (field.Name.StartsWith('<') || field.Name.Contains("k__BackingField", StringComparison.Ordinal) || field.IsSpecialName)
                    continue;
                var modifier = field.IsStatic ? "static " : "";
                var value = field.IsLiteral ? $" = {field.GetRawConstantValue()}" : "";
                members.Add($"  FIELD {modifier}{field.Name} : {DescribeType(field.FieldType)}{value}");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                // Property accessors and operator overloads are covered elsewhere or are special.
                if (method.IsSpecialName)
                    continue;
                var modifier = method.IsStatic ? "static " : "";
                members.Add($"  METHOD {modifier}{method.Name}({DescribeParameters(method.GetParameters())}) : {DescribeType(method.ReturnType)}");
            }

            foreach (var line in members.OrderBy(m => m, StringComparer.Ordinal))
                builder.Append(line).Append('\n');
        }

        return builder.ToString();
    }

    private static string DescribeTypeKind(Type type)
    {
        if (type.IsEnum)
            return "enum";
        if (type.IsInterface)
            return "interface";
        if (type.IsValueType)
            return type.IsDefined(typeof(System.Runtime.CompilerServices.IsByRefLikeAttribute), false) ? "ref struct" : "struct";
        if (type.IsAbstract && type.IsSealed)
            return "static class";
        if (type.IsAbstract)
            return "abstract class";
        return type.IsSealed ? "sealed class" : "class";
    }

    private static string DescribeParameters(ParameterInfo[] parameters)
        => string.Join(", ", parameters.Select(p => $"{DescribeType(p.ParameterType)} {p.Name}{(p.HasDefaultValue ? $" = {FormatDefault(p.DefaultValue)}" : "")}"));

    private static string FormatDefault(object? value) => value switch
    {
        null => "null",
        // Escaped so a default containing a newline cannot break the one-member-per-line format.
        string s => "\"" + s.Replace("\\", "\\\\", StringComparison.Ordinal)
                            .Replace("\r", "\\r", StringComparison.Ordinal)
                            .Replace("\n", "\\n", StringComparison.Ordinal)
                            .Replace("\t", "\\t", StringComparison.Ordinal) + "\"",
        bool b => b ? "true" : "false",
        _ => value.ToString() ?? "null",
    };

    private static string DescribeType(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
                name = name.Substring(0, tick);
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(DescribeType))}>";
        }

        if (type.IsArray)
            return $"{DescribeType(type.GetElementType()!)}[]";
        if (type.IsByRef)
            return "ref " + DescribeType(type.GetElementType()!);
        return type.FullName ?? type.Name;
    }
}
