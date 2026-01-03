namespace Tsukikage.OCR.Tsukikage;

internal sealed class Paragraph(in BoundingBox boundingBox, Line[] lines, WritingDirection writingDirection, string text)
{
    public BoundingBox BoundingBox { get; } = boundingBox;
    public Line[] Lines { get; } = lines;
    public WritingDirection WritingDirection { get; } = writingDirection;
    public string Text { get; set; } = text;
}
