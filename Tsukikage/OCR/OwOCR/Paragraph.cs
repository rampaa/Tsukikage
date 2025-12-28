using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class Paragraph(BoundingBox boundingBox, Line[] lines, WritingDirection writingDirection = WritingDirection.None)
{
    [JsonPropertyName("bounding_box")] public BoundingBox BoundingBox { get; } = boundingBox;
    [JsonPropertyName("lines")] public Line[] Lines { get; } = lines;
    [JsonPropertyName("writing_direction")] public WritingDirection WritingDirection { get; } = writingDirection;
    [JsonIgnore] public string Text { get; set; } = string.Join("", lines.Select(static line => line.Text));
}
