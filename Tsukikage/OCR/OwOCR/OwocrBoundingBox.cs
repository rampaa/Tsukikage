using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class OwocrBoundingBox(float centerX, float centerY, float width, float height, float? rotationZ) : IEquatable<OwocrBoundingBox>
{
    [JsonPropertyName("center_x")] public float CenterX { get; } = centerX;
    [JsonPropertyName("center_y")] public float CenterY { get; } = centerY;
    [JsonPropertyName("width")] public float Width { get; } = width;
    [JsonPropertyName("height")] public float Height { get; } = height;

    // ReSharper disable once MemberCanBePrivate.Global
    [JsonPropertyName("rotation_z")] public float? RotationZ { get; } = rotationZ;

    [JsonIgnore] public float CosNegativeRotation { get; } = MathF.Cos(-rotationZ ?? 0);
    [JsonIgnore] public float SinNegativeRotation { get; } = MathF.Sin(-rotationZ ?? 0);

    public override int GetHashCode()
    {
        return HashCode.Combine(CenterX, CenterY, Width, Height, RotationZ);
    }

    public override bool Equals(object? obj)
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        return obj is OwocrBoundingBox other
            && CenterX == other.CenterX
            && CenterY == other.CenterY
            && Width == other.Width
            && Height == other.Height
            && RotationZ == other.RotationZ;
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    public bool Equals(OwocrBoundingBox? other)
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        return other is not null
            && CenterX == other.CenterX
            && CenterY == other.CenterY
            && Width == other.Width
            && Height == other.Height
            && RotationZ == other.RotationZ;
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    public static bool operator ==(OwocrBoundingBox? left, OwocrBoundingBox? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(OwocrBoundingBox? left, OwocrBoundingBox? right) => left?.Equals(right) ?? right is not null;
}
