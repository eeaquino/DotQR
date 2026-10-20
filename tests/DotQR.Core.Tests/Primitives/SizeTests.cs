namespace DotQRTests.Core.Primitives;

public class SizeTests
{
    [Fact]
    public void Constructor_stores_width_and_height()
    {
        var size = new Size(21, 42);
        size.Width.ShouldBe(21);
        size.Height.ShouldBe(42);
    }

    [Fact]
    public void Empty_is_zero_and_IsEmpty_is_true_for_it()
    {
        Size.Empty.Width.ShouldBe(0);
        Size.Empty.Height.ShouldBe(0);
        Size.Empty.IsEmpty.ShouldBeTrue();
        new Size(1, 0).IsEmpty.ShouldBeFalse();
        new Size(0, 0).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Equality_compares_both_dimensions()
    {
        (new Size(3, 4) == new Size(3, 4)).ShouldBeTrue();
        (new Size(3, 4) == new Size(4, 3)).ShouldBeFalse();
        (new Size(3, 4) != new Size(4, 3)).ShouldBeTrue();
        new Size(3, 4).GetHashCode().ShouldBe(new Size(3, 4).GetHashCode());
    }

    [Fact]
    public void ToString_reports_both_dimensions()
    {
        new Size(3, 4).ToString().ShouldBe("3x4");
    }
}
