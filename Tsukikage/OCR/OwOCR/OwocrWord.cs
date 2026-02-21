using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed record class OwocrWord(string Text, OwocrBoundingBox BoundingBox, string? Separator, OwocrSymbol[]? Symbols = null)
{
    [JsonPropertyName("bounding_box")] public OwocrBoundingBox BoundingBox { get; } = BoundingBox;
    [JsonPropertyName("text")] public string Text { get; set; } = Text;
    [JsonPropertyName("separator")] public string? Separator { get; } = Separator;
    [JsonPropertyName("symbols")] public OwocrSymbol[]? Symbols { get; } = Symbols;
}
