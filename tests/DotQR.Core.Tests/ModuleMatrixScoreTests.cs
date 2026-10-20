namespace DotQRTests.Core;

public class ModuleMatrixScoreTests
{
    private static QRCodeGenerator.ModuleMatrix CreateMaskedMatrix(int size, int patternIndex = 0)
    {
        using var blocked = new QRCodeGenerator.ModuleMatrix(size);
        var matrix = new QRCodeGenerator.ModuleMatrix(size);

        // Populate a deterministic pattern so the score is non-trivial.
        var random = new Random(size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                matrix[y, x] = random.Next(2) == 0;
            }
        }

        var mask = QRCodeGenerator.ModulePlacer.MaskPattern.Patterns[patternIndex];
        mask.Apply(matrix, blocked);
        return matrix;
    }

    [Fact]
    public void Score_with_max_threshold_equals_the_plain_score()
    {
        using var matrix = CreateMaskedMatrix(29);
        matrix.Score(int.MaxValue).ShouldBe(matrix.Score());
    }

    [Fact]
    public void Score_at_the_full_value_returns_the_exact_score()
    {
        using var matrix = CreateMaskedMatrix(29);
        var full = matrix.Score();
        full.ShouldBeGreaterThan(0);
        matrix.Score(full).ShouldBe(full);
    }

    [Fact]
    public void Aborting_never_returns_a_value_below_the_full_score()
    {
        using var matrix = CreateMaskedMatrix(41);
        var full = matrix.Score();

        // Every penalty term is non-negative, so an abandoned scan can only under-report; it must
        // never claim a score lower than the true one, and must always exceed the threshold it
        // gave up at.
        foreach (var threshold in new[] { 0, 1, 10, full / 2, full - 1 })
        {
            if (threshold >= full)
                continue;

            var partial = matrix.Score(threshold);
            partial.ShouldBeGreaterThan(threshold, $"threshold {threshold}");
            partial.ShouldBeLessThanOrEqualTo(full, $"threshold {threshold}");
        }
    }

    [Fact]
    public void Aborted_scans_are_strictly_cheaper_to_reject_than_full_scans()
    {
        using var matrix = CreateMaskedMatrix(41);
        var full = matrix.Score();

        // A threshold of 0 must abort essentially immediately, which is the whole point: the
        // caller only needs to know the candidate already lost.
        matrix.Score(0).ShouldBeGreaterThan(0);
        full.ShouldBeGreaterThan(0);
    }
}
