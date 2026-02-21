namespace Tsukikage.OCR.Tsukikage;

internal sealed record class Grapheme(string Text, in BoundingBox BoundingBox)
{
    public BoundingBox BoundingBox { get; } = BoundingBox;
    public string Text { get; } = Text;
}
