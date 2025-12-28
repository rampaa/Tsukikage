using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tsukikage.Interop;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal readonly struct ImageProperties(int width, int height, int x, int y) : IEquatable<ImageProperties>
{
    [JsonPropertyName("width")] public int Width { get; } = width;
    [JsonPropertyName("height")] public int Height { get; } = height;
    [JsonPropertyName("x")] public int X { get; } = x;
    [JsonPropertyName("y")] public int Y { get; } = y;

    public bool Contains(Point point)
    {
        return X <= point.X
            && Y <= point.Y
            && X + Width > point.X
            && Y + Height > point.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height, X, Y);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ImageProperties imageProperties && Equals(imageProperties);
    }

    public bool Equals(ImageProperties other)
    {
        return Width == other.Width && Height == other.Height && X == other.X && Y == other.Y;
    }

    public static bool operator ==(ImageProperties left, ImageProperties right) => left.Equals(right);
    public static bool operator !=(ImageProperties left, ImageProperties right) => !left.Equals(right);

    public override string ToString()
    {
        return $"Width: {Width}, Height: {Height}, X: {X}, Y: {Y}";
    }
}
