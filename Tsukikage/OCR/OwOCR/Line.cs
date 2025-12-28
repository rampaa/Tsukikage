using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal sealed class Line(BoundingBox boundingBox, Word[] words, string? text)
{
    [JsonPropertyName("bounding_box")] public BoundingBox BoundingBox { get; } = boundingBox;
    [JsonPropertyName("words")] public Word[] Words { get; } = words;
    [JsonPropertyName("text")] public string Text { get; } = text ?? string.Join("", words.Select(static word => word.Text + word.Separator));
}
