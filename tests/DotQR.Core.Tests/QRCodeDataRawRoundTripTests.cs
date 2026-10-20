namespace DotQRTests.Core;

public class QRCodeDataRawRoundTripTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20)]
    [InlineData(40)]
    public void Raw_data_round_trips_preserving_the_module_matrix(int version)
    {
        using var generator = new QRCodeGenerator();
        using var original = generator.CreateQrCode("raw round trip", QRCodeGenerator.ECCLevel.M, requestedVersion: version);

        var raw = original.GetRawData(QRCodeData.Compression.Uncompressed);
        using var restored = new QRCodeData(raw, QRCodeData.Compression.Uncompressed);

        restored.Version.ShouldBe(original.Version);
        restored.ModuleMatrix.Count.ShouldBe(original.ModuleMatrix.Count);
        for (var y = 0; y < original.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < original.ModuleMatrix[y].Length; x++)
            {
                restored.ModuleMatrix[y][x].ShouldBe(original.ModuleMatrix[y][x], $"module {x},{y}");
            }
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-3)]
    [InlineData(-4)]
    public void Micro_sized_matrices_round_trip(int microVersion)
    {
        // Micro QR generation via CreateQrCode(requestedVersion:) is broken upstream, so exercise
        // the raw format's Micro side-length encoding (sideLen < 29) directly.
        using var original = new QRCodeData(microVersion, addPadding: true);

        var random = new Random(microVersion);
        for (var y = 0; y < original.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < original.ModuleMatrix[y].Length; x++)
            {
                if (random.Next(2) == 0)
                    original.ModuleMatrix[y][x] = true;
            }
        }

        var raw = original.GetRawData(QRCodeData.Compression.Uncompressed);
        using var restored = new QRCodeData(raw, QRCodeData.Compression.Uncompressed);

        restored.Version.ShouldBe(microVersion);
        for (var y = 0; y < original.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < original.ModuleMatrix[y].Length; x++)
            {
                restored.ModuleMatrix[y][x].ShouldBe(original.ModuleMatrix[y][x], $"module {x},{y}");
            }
        }
    }

    [Theory]
    [InlineData(QRCodeData.Compression.Deflate)]
    [InlineData(QRCodeData.Compression.GZip)]
    public void Compressed_raw_data_round_trips(QRCodeData.Compression compression)
    {
        using var generator = new QRCodeGenerator();
        using var original = generator.CreateQrCode("compressed raw round trip", QRCodeGenerator.ECCLevel.Q);

        var raw = original.GetRawData(compression);
        using var restored = new QRCodeData(raw, compression);

        for (var y = 0; y < original.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < original.ModuleMatrix[y].Length; x++)
            {
                restored.ModuleMatrix[y][x].ShouldBe(original.ModuleMatrix[y][x], $"module {x},{y}");
            }
        }
    }
}
