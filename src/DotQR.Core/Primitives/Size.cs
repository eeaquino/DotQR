namespace DotQR;

/// <summary>
/// Represents an integer width and height.
/// </summary>
public readonly struct Size : IEquatable<Size>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> struct.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Gets the width.</summary>
    public int Width { get; }

    /// <summary>Gets the height.</summary>
    public int Height { get; }

    /// <summary>Gets a size with zero width and height.</summary>
    public static Size Empty => default;

    /// <summary>Gets a value indicating whether both dimensions are zero.</summary>
    public bool IsEmpty => Width == 0 && Height == 0;

    /// <inheritdoc/>
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Size other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Width, Height);

    /// <inheritdoc/>
    public override string ToString() => $"{Width}x{Height}";

    /// <summary>Compares two sizes for equality.</summary>
    /// <param name="left">The left size.</param>
    /// <param name="right">The right size.</param>
    /// <returns><see langword="true"/> if both dimensions match.</returns>
    public static bool operator ==(Size left, Size right) => left.Equals(right);

    /// <summary>Compares two sizes for inequality.</summary>
    /// <param name="left">The left size.</param>
    /// <param name="right">The right size.</param>
    /// <returns><see langword="true"/> if either dimension differs.</returns>
    public static bool operator !=(Size left, Size right) => !left.Equals(right);
}
