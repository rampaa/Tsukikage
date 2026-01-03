using Tsukikage.Interop;

namespace Tsukikage.OCR.Tsukikage;

internal sealed class OcrResult(Rectangle imageProperties, int windowHandle, Paragraph[] paragraphs)
{
    public Rectangle ImageProperties { get; } = imageProperties;
    public int WindowHandle { get; } = windowHandle;
    public Paragraph[] Paragraphs { get; } = paragraphs;
}
