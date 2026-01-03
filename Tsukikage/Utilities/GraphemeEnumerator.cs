using System.Globalization;

namespace Tsukikage.Utilities;

internal ref struct GraphemeEnumerator
{
    private ReadOnlySpan<char> _remaining;
    private int Length { get; set; }

    public ReadOnlySpan<char> Current { get; private set; }

    public GraphemeEnumerator(ReadOnlySpan<char> text)
    {
        _remaining = text;
        Length = 0;
    }

    public bool MoveNext()
    {
        if (_remaining.IsEmpty)
        {
            return false;
        }

        Length = StringInfo.GetNextTextElementLength(_remaining);
        Current = _remaining[..Length];
        _remaining = _remaining[Length..];
        return true;
    }
}
