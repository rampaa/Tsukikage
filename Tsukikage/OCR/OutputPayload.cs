namespace Tsukikage.OCR;

internal enum OutputPayload
{
    GraphemeInfo = 0,
    Paragraph,
    TextStartingFromPosition,
    Line,
    Word,
    // ReSharper disable once UnusedMember.Global
    Grapheme
}
