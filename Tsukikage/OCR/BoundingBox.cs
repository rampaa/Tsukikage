using System.Text.Json.Serialization;

namespace Tsukikage.OCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class BoundingBox(float centerX, float centerY, float width, float height, float? rotationZ) : IEquatable<BoundingBox>
{
    [JsonPropertyName("center_x")] public float CenterX { get; } = centerX;
    [JsonPropertyName("center_y")] public float CenterY { get; } = centerY;
    [JsonPropertyName("width")] public float Width { get; } = width;
    [JsonPropertyName("height")] public float Height { get; } = height;

    // ReSharper disable once MemberCanBePrivate.Global
    [JsonPropertyName("rotation_z")] public float? RotationZ { get; } = rotationZ;

    [JsonIgnore] public float CosInverseRotation { get; } = MathF.Cos(-rotationZ ?? 0);
    [JsonIgnore] public float SinInverseRotation { get; } = MathF.Sin(-rotationZ ?? 0);

    public override int GetHashCode()
    {
        return HashCode.Combine(CenterX, CenterY, Width, Height, RotationZ);
    }

    public override bool Equals(object? obj)
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        return obj is BoundingBox other
            && CenterX == other.CenterX
            && CenterY == other.CenterY
            && Width == other.Width
            && Height == other.Height
            && RotationZ == other.RotationZ;
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    public bool Equals(BoundingBox? other)
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

    public static bool operator ==(BoundingBox? left, BoundingBox? right) => left?.Equals(right) ?? (right is null);

    public static bool operator !=(BoundingBox? left, BoundingBox? right) => left?.Equals(right) ?? right is not null;
}
