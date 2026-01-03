using Tsukikage.Interop;

namespace Tsukikage.OCR.Tsukikage;

internal sealed class OcrResult(Rectangle imageProperties, Paragraph[] paragraphs)
{
    public Rectangle ImageProperties { get; } = imageProperties;
    public Paragraph[] Paragraphs { get; } = paragraphs;
}
