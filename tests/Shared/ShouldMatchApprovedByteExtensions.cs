using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shouldly;

/// <summary>
/// Binary approval support for tests that do not need to decode or re-encode an image.
/// Compares the produced bytes against an approved file next to the test source file.
/// </summary>
/// <remarks>
/// Unlike Shouldly's built-in string approvals, this overload never auto-approves: a missing or
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

        var approvedFile = Path.Combine(outputFolder, $"{className}.{testMethodName}{discriminatorPart}.approved.{fileExtension}");
        var receivedFile = Path.Combine(outputFolder, $"{className}.{testMethodName}{discriminatorPart}.received.{fileExtension}");

        File.WriteAllBytes(receivedFile, actual);

        if (!File.Exists(approvedFile))
        {
            throw new ShouldAssertException($"""
                Approval file {approvedFile}
                    does not exist

                To approve the received file, run:

                copy /Y "{receivedFile}" "{approvedFile}"

                """);
        }

        var approvedFileContents = File.ReadAllBytes(approvedFile);
        var receivedFileContents = File.ReadAllBytes(receivedFile);

        var contentsMatch = approvedFileContents.AsSpan().SequenceEqual(receivedFileContents.AsSpan());

        if (!contentsMatch)
        {
            var baseMessage = $"Binary files do not match. Expected length: {approvedFileContents.Length}, Actual length: {receivedFileContents.Length}";
            var copyCommand = $"""


                To approve the received file, run:

                copy /Y "{receivedFile}" "{approvedFile}"

                """;

            var message = customMessage != null
                ? $"{customMessage}\n\n{baseMessage}{copyCommand}"
                : $"{baseMessage}{copyCommand}";

            throw new ShouldAssertException(message);
        }

        File.Delete(receivedFile);
    }
}
