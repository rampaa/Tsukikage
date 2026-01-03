using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal readonly record struct OwocrImageProperties(int Width, int Height, int? X, int? Y, int? WindowHandle, float? ScaleX = 1f, float? ScaleY = 1f)
{
    [JsonPropertyName("width")] public int Width { get; } = Width;
    [JsonPropertyName("height")] public int Height { get; } = Height;
    [JsonPropertyName("x")] public int? X { get; } = X ?? 0;
    [JsonPropertyName("y")] public int? Y { get; } = Y ?? 0;
    [JsonPropertyName("window_handle")] public int? WindowHandle { get; } = WindowHandle ?? 0;
    [JsonPropertyName("scale_x")] public float? ScaleX { get; } = ScaleX ?? 1f;
    [JsonPropertyName("scale_y")] public float? ScaleY { get; } = ScaleY ?? 1f;
}
