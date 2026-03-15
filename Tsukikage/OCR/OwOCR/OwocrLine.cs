using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal sealed class OwocrLine(OwocrBoundingBox boundingBox, OwocrWord[] words, OwocrWritingDirection? writingDirection = null)
{
    [JsonPropertyName("bounding_box")] public OwocrBoundingBox BoundingBox { get; } = boundingBox;
    [JsonPropertyName("words")] public OwocrWord[] Words { get; } = words;
    [JsonPropertyName("writing_direction")] public OwocrWritingDirection? WritingDirection { get; } = writingDirection;
}
