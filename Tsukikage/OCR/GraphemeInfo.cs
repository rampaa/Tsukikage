using System.Text.Json.Serialization;

namespace Tsukikage.OCR;

internal sealed record class GraphemeInfo(int GraphemeStartIndex, string Text, bool IsVertical)
{
    [JsonPropertyName("i")] public int GraphemeStartIndex { get; } = GraphemeStartIndex;
    [JsonPropertyName("t")] public string Text { get; } = Text;
    [JsonPropertyName("v")] public bool IsVertical { get; } = IsVertical;
}
