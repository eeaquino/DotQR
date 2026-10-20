using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shouldly;

/// <summary>
/// Binary and text approval support for tests that do not need to decode or re-encode an artifact.
/// Compares the produced content against an approved file next to the test source file.
/// </summary>
/// <remarks>
/// Unlike Shouldly's built-in string approvals, these overloads never auto-approve: a missing or
/// differing baseline always fails, so a regression cannot silently rewrite its own expectation.
/// </remarks>
[ShouldlyMethods]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ShouldMatchApprovedByteExtensions
{
    /// <summary>
    /// Asserts that the produced bytes match the approved file for the calling test method.
    /// </summary>
    /// <param name="actual">The produced bytes.</param>
    /// <param name="fileExtension">Extension of the approved file, without a leading dot.</param>
    /// <param name="discriminator">Optional discriminator appended to the file name.</param>
    /// <param name="customMessage">Optional message prepended to a failure.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldMatchApproved(this byte[] actual, string fileExtension, string? discriminator = null, string? customMessage = null)
    {
        ArgumentNullException.ThrowIfNull(actual);

        var (approvedFile, receivedFile) = ResolvePaths(fileExtension, discriminator);
        File.WriteAllBytes(receivedFile, actual);

        if (!File.Exists(approvedFile))
            throw MissingApproval(approvedFile, receivedFile);

        var approved = File.ReadAllBytes(approvedFile);
        var received = File.ReadAllBytes(receivedFile);

        if (!approved.AsSpan().SequenceEqual(received.AsSpan()))
        {
            var baseMessage = $"Binary files do not match. Expected length: {approved.Length}, Actual length: {received.Length}";
            throw Mismatch(baseMessage, approvedFile, receivedFile, customMessage);
        }

        File.Delete(receivedFile);
    }

    /// <summary>
    /// Asserts that the produced text matches the approved file for the calling test method,
    /// treating CRLF and LF as equivalent.
    /// </summary>
    /// <remarks>
    /// Some formats legitimately carry platform-dependent line terminators — PostScript embeds the
    /// line endings of the source file inside raw string literals, and git may rewrite them on
    /// checkout. Comparing with line endings normalized keeps that out of the assertion while
    /// still catching any real content change.
    /// </remarks>
    /// <param name="actual">The produced text.</param>
    /// <param name="fileExtension">Extension of the approved file, without a leading dot.</param>
    /// <param name="discriminator">Optional discriminator appended to the file name.</param>
    /// <param name="customMessage">Optional message prepended to a failure.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldMatchApprovedText(this string actual, string fileExtension, string? discriminator = null, string? customMessage = null)
    {
        ArgumentNullException.ThrowIfNull(actual);

        var (approvedFile, receivedFile) = ResolvePaths(fileExtension, discriminator);
        var normalized = NormalizeNewlines(actual);
        File.WriteAllText(receivedFile, normalized);

        if (!File.Exists(approvedFile))
            throw MissingApproval(approvedFile, receivedFile);

        var approved = NormalizeNewlines(File.ReadAllText(approvedFile));

        if (!string.Equals(approved, normalized, StringComparison.Ordinal))
        {
            var index = FirstDifference(approved, normalized);
            var baseMessage = $"Text files do not match. Expected length: {approved.Length}, Actual length: {normalized.Length}, first difference at index {index}.";
            throw Mismatch(baseMessage, approvedFile, receivedFile, customMessage);
        }

        File.Delete(receivedFile);
    }

    private static string NormalizeNewlines(string value) => value.Replace("\r\n", "\n").Replace("\r", "\n");

    private static int FirstDifference(string left, string right)
    {
        var limit = Math.Min(left.Length, right.Length);
        for (var i = 0; i < limit; i++)
        {
            if (left[i] != right[i])
                return i;
        }
        return limit;
    }

    private static ShouldAssertException MissingApproval(string approvedFile, string receivedFile) =>
        new($"""
            Approval file {approvedFile}
                does not exist

            To approve the received file, run:

            copy /Y "{receivedFile}" "{approvedFile}"

            """);

    private static ShouldAssertException Mismatch(string baseMessage, string approvedFile, string receivedFile, string? customMessage) =>
        new((customMessage is null ? baseMessage : $"{customMessage}\n\n{baseMessage}") + $"""


            To approve the received file, run:

            copy /Y "{receivedFile}" "{approvedFile}"

            """);

    private static (string ApprovedFile, string ReceivedFile) ResolvePaths(string fileExtension, string? discriminator)
    {
        var stackTrace = new StackTrace(true);

        // Find the first frame that is NOT in a class marked with [ShouldlyMethods].
        StackFrame? frame = null;
        MethodBase? method = null;

        for (var i = 1; i < stackTrace.FrameCount; i++)
        {
            var currentFrame = stackTrace.GetFrame(i);
            var currentMethod = currentFrame?.GetMethod();
            var declaringType = currentMethod?.DeclaringType;

            var hasShouldlyAttribute = declaringType?.GetCustomAttributes(typeof(ShouldlyMethodsAttribute), false).Length > 0;
            if (!hasShouldlyAttribute)
            {
                frame = currentFrame;
                method = currentMethod;
                break;
            }
        }

        if (frame == null)
            throw new ShouldAssertException("Unable to get stack frame information");
        if (method == null)
            throw new ShouldAssertException("Unable to get method information from stack frame");

        var fileName = frame.GetFileName();
        if (string.IsNullOrEmpty(fileName))
            throw new ShouldAssertException($"Source information not available, make sure you are compiling with full debug information. Method: {method.DeclaringType?.Name}.{method.Name}");

        var outputFolder = Path.GetDirectoryName(fileName);
        if (string.IsNullOrEmpty(outputFolder))
            throw new ShouldAssertException($"Unable to determine output folder from file: {fileName}");

        var className = method.DeclaringType?.Name ?? throw new ShouldAssertException("Unable to get class name from method information");
        var testMethodName = method.Name;
        var discriminatorPart = string.IsNullOrEmpty(discriminator) ? "" : $".{discriminator}";

        return (
            Path.Combine(outputFolder, $"{className}.{testMethodName}{discriminatorPart}.approved.{fileExtension}"),
            Path.Combine(outputFolder, $"{className}.{testMethodName}{discriminatorPart}.received.{fileExtension}"));
    }
}
