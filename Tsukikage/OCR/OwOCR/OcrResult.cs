using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal sealed class OcrResult(ImageProperties imageProperties, Paragraph[] paragraphs) : IEquatable<OcrResult>
{
    [JsonPropertyName("image_properties")] public ImageProperties ImageProperties { get; } = imageProperties;
    [JsonPropertyName("paragraphs")] public Paragraph[] Paragraphs { get; } = paragraphs;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + ImageProperties.GetHashCode();
            foreach (Paragraph paragraph in Paragraphs)
            {
                hash = (hash * 37) + paragraph.GetHashCode();
            }

            return hash;
        }
    }

    public bool Equals(OcrResult? other)
    {
        return other is not null && ImageProperties == other.ImageProperties && Paragraphs.SequenceEqual(other.Paragraphs);
    }

    public override bool Equals(object? obj)
    {
        return obj is OcrResult other && ImageProperties == other.ImageProperties && Paragraphs.SequenceEqual(other.Paragraphs);
    }

    public static bool operator ==(OcrResult? left, OcrResult? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(OcrResult? left, OcrResult? right) => !(left == right);
}
