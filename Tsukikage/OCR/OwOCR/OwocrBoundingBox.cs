using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed record class OwocrBoundingBox(float CenterX, float CenterY, float Width, float Height, float? RotationZ)
{
    [JsonPropertyName("center_x")] public float CenterX { get; } = CenterX;
    [JsonPropertyName("center_y")] public float CenterY { get; } = CenterY;
    [JsonPropertyName("width")] public float Width { get; } = Width;
    [JsonPropertyName("height")] public float Height { get; } = Height;
    [JsonPropertyName("rotation_z")] public float? RotationZ { get; } = RotationZ;
}
