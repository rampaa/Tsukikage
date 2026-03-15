namespace Tsukikage.OCR.Tsukikage;

internal sealed class Line(in BoundingBox boundingBox, Word[] words, string text, WritingDirection writingDirection)
{
    public BoundingBox BoundingBox { get; } = boundingBox;
    public Word[] Words { get; } = words;
    public string Text { get; set; } = text;
    public WritingDirection WritingDirection { get; } = writingDirection;
}
