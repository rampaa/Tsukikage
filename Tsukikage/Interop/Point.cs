using System.Runtime.InteropServices;

namespace Tsukikage.Interop;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Point(int x, int y) : IEquatable<Point>
{
    public readonly int X = x;
    public readonly int Y = y;

    public bool Equals(Point other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Point point && Equals(point);
    }

    public static bool operator ==(Point left, Point right) => left.Equals(right);
    public static bool operator !=(Point left, Point right) => !left.Equals(right);

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
