using System.Text.Json.Serialization;

namespace Tsukikage.OCR.OwOCR;

[method: JsonConstructor]
internal sealed class OwocrOcrResult(in OwocrImageProperties imageProperties, OwocrParagraph[] paragraphs)
{
    [JsonPropertyName("image_properties")] public OwocrImageProperties ImageProperties { get; } = imageProperties;
    [JsonPropertyName("paragraphs")] public OwocrParagraph[] Paragraphs { get; } = paragraphs;
}
