using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed record class OwocrWord(string Text, OwocrBoundingBox BoundingBox, string? Separator)
{
    [JsonPropertyName("bounding_box")] public OwocrBoundingBox BoundingBox { get; } = BoundingBox;
    [JsonPropertyName("text")] public string Text { get; set; } = Text;

    // ReSharper disable once MemberCanBePrivate.Global
    [JsonPropertyName("separator")] public string Separator { get; } = Separator ?? "";
}
