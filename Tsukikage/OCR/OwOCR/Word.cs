using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class Word(string text, BoundingBox boundingBox, string? separator) : IEquatable<Word>
{
    [JsonPropertyName("text")] public string Text { get; } = text;
    [JsonPropertyName("bounding_box")] public BoundingBox BoundingBox { get; } = boundingBox;

    // ReSharper disable once MemberCanBePrivate.Global
    [JsonPropertyName("separator")] public string Separator { get; } = separator ?? "";

    public bool Equals(Word? other)
    {
        return other is not null
            && Text == other.Text
            && BoundingBox == other.BoundingBox
            && Separator == other.Separator;
    }

    public override bool Equals(object? obj)
    {
        return obj is Word other
            && Text == other.Text
            && BoundingBox == other.BoundingBox
            && Separator == other.Separator;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Text.GetHashCode(StringComparison.Ordinal), BoundingBox, Separator.GetHashCode(StringComparison.Ordinal));
    }
}
