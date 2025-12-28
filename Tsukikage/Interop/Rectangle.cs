using System.Diagnostics.CodeAnalysis;

namespace Tsukikage.Interop;

internal readonly struct Rectangle : IEquatable<Rectangle>
{
    public int X { get; }
    public int Y { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public int Width { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public int Height { get; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    //public int Left => X;
    //public int Top => Y;
    //public int Right => X + Width;
    //public int Bottom => Y + Height;

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Rectangle rectangle && Equals(rectangle);
    }

    public bool Equals(Rectangle other)
    {
        return X == other.X
            && Y == other.Y
            && Width == other.Width
            && Height == other.Height;
    }

    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
    public static bool operator !=(Rectangle left, Rectangle right) => !(left == right);

    // ReSharper disable once MemberCanBePrivate.Global
    public bool Contains(int x, int y)
    {
        return X <= x
            && x < X + Width
            && Y <= y
            && y < Y + Height;
    }

    public bool Contains(Point pt)
    {
        return Contains(pt.X, pt.Y);
    }

    public override string ToString()
    {
        return $"{{X={X},Y={Y},Width={Width},Height={Height}}}";
    }
}
