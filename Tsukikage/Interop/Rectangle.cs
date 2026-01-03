namespace Tsukikage.Interop;

internal readonly record struct Rectangle
{
    public int X { get; }
    public int Y { get; }
    private int Right { get; }
    private int Bottom { get; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Right = x + width;
        Bottom = y + height;
    }

    public bool Contains(Point point)
    {
        return X <= point.X
            && Y <= point.Y
            && Right > point.X
            && Bottom > point.Y;
    }
}
