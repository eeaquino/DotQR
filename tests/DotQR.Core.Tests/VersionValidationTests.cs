namespace DotQRTests.Core;

/// <summary>
/// Version selection and validation for the standard and Micro QR generators.
/// </summary>
public class VersionValidationTests
{
    [Theory]
    [InlineData(-5)]
    [InlineData(-4)]
    [InlineData(-3)]
    [InlineData(-2)]
    [InlineData(0)]
    [InlineData(41)]
    [InlineData(100)]
    public void CreateQrCode_rejects_versions_outside_the_standard_range(int requestedVersion)
    {
        using var generator = new QRCodeGenerator();

        // Only -1 (automatic) and 1-40 are meaningful for standard QR codes. Micro versions are
        // reached through GenerateMicroQrCode, so anything else must be rejected up front rather
        // than surfacing an unrelated dictionary or capacity exception from deep inside selection.
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => generator.CreateQrCode("test", QRCodeGenerator.ECCLevel.M, requestedVersion: requestedVersion));

        exception.ActualValue.ShouldBe(requestedVersion);
        exception.Message.ShouldContain("requestedVersion");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(40)]
    public void CreateQrCode_accepts_valid_versions(int requestedVersion)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("test", QRCodeGenerator.ECCLevel.M, requestedVersion: requestedVersion);
        data.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(-2, 21)]
    [InlineData(-3, 23)]
    [InlineData(-4, 25)]
    public void GenerateMicroQrCode_supports_explicit_micro_versions(int requestedVersion, int expectedModules)
    {
        // Locks in that explicit Micro versions really do work through the Micro entry point.
        using var data = QRCodeGenerator.GenerateMicroQrCode("12345", QRCodeGenerator.ECCLevel.L, requestedVersion);

        data.Version.ShouldBe(requestedVersion);
        data.ModuleMatrix.Count.ShouldBe(expectedModules);
    }

    [Fact]
    public void GenerateMicroQrCode_supports_byte_mode_from_m3()
    {
        using var m3 = QRCodeGenerator.GenerateMicroQrCode("abc", QRCodeGenerator.ECCLevel.L, -3);
        m3.ModuleMatrix.Count.ShouldBe(23);

        // M2 has no byte mode, so it must reject the payload rather than emit anything.
        Should.Throw<InvalidOperationException>(
            () => QRCodeGenerator.GenerateMicroQrCode("abc", QRCodeGenerator.ECCLevel.L, -2));
    }
}
