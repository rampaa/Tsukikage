using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class OwocrParagraph(OwocrBoundingBox boundingBox, OwocrLine[] lines, OwocrWritingDirection? writingDirection)
{
    [JsonPropertyName("bounding_box")] public OwocrBoundingBox BoundingBox { get; } = boundingBox;
    [JsonPropertyName("lines")] public OwocrLine[] Lines { get; } = lines;
    [JsonPropertyName("writing_direction")] public OwocrWritingDirection? WritingDirection { get; } = writingDirection;
}
