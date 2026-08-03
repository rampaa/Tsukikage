namespace Tsukikage.OCR.Tsukikage;

internal sealed record class Grapheme(string Text, in BoundingBox BoundingBox, int GraphemeCount, int SeparatorGraphemeCount, int SeparatorCharLength)
{
    public BoundingBox BoundingBox { get; } = BoundingBox;
    public string Text { get; set; } = Text;
    public int GraphemeCount { get; } = GraphemeCount;
    public int SeparatorGraphemeCount { get; } = SeparatorGraphemeCount;
    public int SeparatorCharLength { get; set; } = SeparatorCharLength;
}
