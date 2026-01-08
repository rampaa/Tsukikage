using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal readonly record struct OwocrImageProperties(int Width, int Height, int? X, int? Y, int? WindowHandle)
{
    [JsonPropertyName("width")] public int Width { get; } = Width;
    [JsonPropertyName("height")] public int Height { get; } = Height;
    [JsonPropertyName("x")] public int? X { get; } = X ?? 0;
    [JsonPropertyName("y")] public int? Y { get; } = Y ?? 0;
    [JsonPropertyName("window_handle")] public int? WindowHandle { get; } = WindowHandle ?? 0;
}
