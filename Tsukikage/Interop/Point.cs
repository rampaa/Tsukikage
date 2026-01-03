using System.Runtime.InteropServices;

namespace Tsukikage.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct Point(int X, int Y)
{
    public readonly int X = X;
    public readonly int Y = Y;
}
