using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

internal sealed record class OwocrSymbol(string Text, OwocrBoundingBox BoundingBox, string? Separator)
{
    [JsonPropertyName("bounding_box")] public OwocrBoundingBox BoundingBox { get; } = BoundingBox;
    [JsonPropertyName("text")] public string Text { get; set; } = Text;
    [JsonPropertyName("separator")] public string? Separator { get; } = Separator;
}
