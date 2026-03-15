namespace Tsukikage.OCR.Tsukikage;

internal sealed class Paragraph(in BoundingBox boundingBox, Line[] lines)
{
    public BoundingBox BoundingBox { get; } = boundingBox;
    public Line[] Lines { get; } = lines;
    public string Text { get; set; } = string.Join("", lines.Select(static line => line.Text));
}
