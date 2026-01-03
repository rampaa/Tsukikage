using System.Diagnostics;
using Tsukikage.Interop;
using Tsukikage.OCR.OwOCR;
using Tsukikage.OCR.Tsukikage;

namespace Tsukikage.OCR;

internal static class MapperUtils
{
    public static OcrResult OwocrOcrResultToOcrResult(OwocrOcrResult owocrOcrResult)
    {
        Paragraph[] paragraphs = new Paragraph[owocrOcrResult.Paragraphs.Length];
        for (int i = 0; i < paragraphs.Length; i++)
        {
            OwocrParagraph owocrParagraph = owocrOcrResult.Paragraphs[i];
            paragraphs[i] = OwocrParagraphToParagraph(owocrParagraph, owocrOcrResult.ImageProperties);
        }

        return new OcrResult(OwocrImagePropertiesTomageProperties(owocrOcrResult.ImageProperties), paragraphs);
    }

    private static Rectangle OwocrImagePropertiesTomageProperties(OwocrImageProperties owocrImageProperties)
    {
        Debug.Assert(owocrImageProperties.X is not null && owocrImageProperties.Y is not null);
        return new Rectangle(owocrImageProperties.X.Value, owocrImageProperties.Y.Value, owocrImageProperties.Width, owocrImageProperties.Height);
    }

    private static Paragraph OwocrParagraphToParagraph(OwocrParagraph owocrParagraph, in OwocrImageProperties imageProperties)
    {
        Line[] lines = new Line[owocrParagraph.Lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            OwocrLine owocrLine = owocrParagraph.Lines[i];
            lines[i] = OwocrLineToLine(owocrLine, imageProperties);
        }

        return new Paragraph(new BoundingBox(owocrParagraph.BoundingBox, imageProperties), lines, OwocrOwocrWritingDirectionToWritingDirection(owocrParagraph.WritingDirection), owocrParagraph.Text);
    }

    private static WritingDirection OwocrOwocrWritingDirectionToWritingDirection(OwocrWritingDirection? owocrWritingDirection)
    {
        return owocrWritingDirection switch
        {
            OwocrWritingDirection.LEFT_TO_RIGHT => WritingDirection.LeftToRightTopToBottom,
            OwocrWritingDirection.TOP_TO_BOTTOM => WritingDirection.TopToBottomRightToLeft,
            _ => WritingDirection.Ambiguous
        };
    }

    private static Line OwocrLineToLine(OwocrLine owocrLine, in OwocrImageProperties imageProperties)
    {
        Word[] words = new Word[owocrLine.Words.Length];
        for (int i = 0; i < words.Length; i++)
        {
            OwocrWord owocrWord = owocrLine.Words[i];
            words[i] = new Word(owocrWord.Text, new BoundingBox(owocrWord.BoundingBox, imageProperties));
        }

        return new Line(new BoundingBox(owocrLine.BoundingBox, imageProperties), words, owocrLine.Text);
    }
}
