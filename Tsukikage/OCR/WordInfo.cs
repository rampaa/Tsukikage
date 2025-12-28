using System.Text.Json.Serialization;

namespace Tsukikage.OCR;

internal sealed class WordInfo(int wordStartIndex, string text, bool isVertical)
{
    [JsonPropertyName("i")] public int WordStartIndex { get; } = wordStartIndex;
    [JsonPropertyName("t")] public string Text { get; } = text;
    [JsonPropertyName("v")] public bool IsVertical { get; } = isVertical;
}
