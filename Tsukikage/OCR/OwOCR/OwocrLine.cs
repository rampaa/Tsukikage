using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal sealed class OwocrLine(OwocrBoundingBox boundingBox, OwocrWord[] words)
{
    [JsonPropertyName("bounding_box")] public OwocrBoundingBox BoundingBox { get; } = boundingBox;
    [JsonPropertyName("words")] public OwocrWord[] Words { get; } = words;
    [JsonIgnore] public string Text { get; } = string.Join("", words.Select(static word => word.Text + word.Separator));
}
