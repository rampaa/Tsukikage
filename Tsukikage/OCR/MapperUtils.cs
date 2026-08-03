using System.Diagnostics;
using System.Text;
using Tsukikage.Interop;
using Tsukikage.OCR.OwOCR;
using Tsukikage.OCR.Tsukikage;
using Tsukikage.Utilities;

namespace Tsukikage.OCR;

internal static class MapperUtils
{
    public static OcrResult ToOcrResult(this OwocrOcrResult owocrOcrResult)
    {
        Paragraph[] paragraphs = new Paragraph[owocrOcrResult.Paragraphs.Length];
        for (int i = 0; i < paragraphs.Length; i++)
        {
            OwocrParagraph owocrParagraph = owocrOcrResult.Paragraphs[i];
            paragraphs[i] = owocrParagraph.ToParagraph(owocrOcrResult.ImageProperties);
        }

        Debug.Assert(owocrOcrResult.ImageProperties.WindowHandle is not null);
        return new OcrResult(owocrOcrResult.ImageProperties.ToRectangle(), owocrOcrResult.ImageProperties.WindowHandle.Value, paragraphs);
    }

    private static Rectangle ToRectangle(this OwocrImageProperties owocrImageProperties)
    {
        Debug.Assert(owocrImageProperties.X is not null && owocrImageProperties.Y is not null);
        return new Rectangle(owocrImageProperties.X.Value, owocrImageProperties.Y.Value, owocrImageProperties.Width, owocrImageProperties.Height);
    }

    private static Paragraph ToParagraph(this OwocrParagraph owocrParagraph, in OwocrImageProperties imageProperties)
    {
        Line[] lines = new Line[owocrParagraph.Lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            OwocrLine owocrLine = owocrParagraph.Lines[i];
            lines[i] = owocrLine.ToLine(imageProperties, owocrParagraph.WritingDirection);
        }

        return new Paragraph(new BoundingBox(owocrParagraph.BoundingBox, imageProperties), lines);
    }

    private static WritingDirection ToWritingDirection(this OwocrWritingDirection? owocrWritingDirection)
    {
        return owocrWritingDirection switch
        {
            OwocrWritingDirection.LEFT_TO_RIGHT => WritingDirection.LeftToRightTopToBottom,
            OwocrWritingDirection.TOP_TO_BOTTOM => WritingDirection.TopToBottomRightToLeft,
            _ => WritingDirection.Ambiguous
        };
    }

    private static Line ToLine(this OwocrLine owocrLine, in OwocrImageProperties imageProperties, OwocrWritingDirection? paragraphWritingDirection)
    {
        StringBuilder lineStringBuilder = new();
        StringBuilder? wordStringBuilder = null;

        Word[] words = new Word[owocrLine.Words.Length];
        for (int i = 0; i < words.Length; i++)
        {
            OwocrWord owocrWord = owocrLine.Words[i];

            Grapheme[]? graphemes = null;
            if (owocrWord.Symbols?.Length > 0)
            {
                if (wordStringBuilder is null)
                {
                    wordStringBuilder = new StringBuilder(owocrWord.Symbols.Length);
                }
                else
                {
                    _ = wordStringBuilder.Clear();
                    _ = wordStringBuilder.EnsureCapacity(owocrWord.Symbols.Length);
                }

                graphemes = new Grapheme[owocrWord.Symbols.Length];
                for (int j = 0; j < graphemes.Length; j++)
                {
                    OwocrSymbol owocrSymbol = owocrWord.Symbols[j];

                    int symbolGraphemeCount = owocrSymbol.Text.GetGraphemeCount();
                    int symbolSeparatorGraphemeCount = owocrSymbol.Separator is not null
                        ? owocrSymbol.Separator.GetGraphemeCount()
                        : 0;

                    int symbolSeparatorCharLength = owocrSymbol.Separator?.Length ?? 0;

                    graphemes[j] = new Grapheme(owocrSymbol.Text, new BoundingBox(owocrSymbol.BoundingBox, imageProperties), symbolGraphemeCount, symbolSeparatorGraphemeCount, symbolSeparatorCharLength);
                    _ = wordStringBuilder.Append(owocrSymbol.Text);
                    if (owocrSymbol.Separator is not null)
                    {
                        _ = wordStringBuilder.Append(owocrSymbol.Separator);
                    }
                }

                owocrWord.Text = wordStringBuilder.ToString();
            }

            int wordGraphemeCount = owocrWord.Text.GetGraphemeCount();
            int wordSeparatorGraphemeCount = owocrWord.Separator is not null
                ? owocrWord.Separator.GetGraphemeCount()
                : 0;

            int wordSeparatorCharLength = owocrWord.Separator?.Length ?? 0;

            words[i] = new Word(owocrWord.Text, new BoundingBox(owocrWord.BoundingBox, imageProperties), graphemes, wordGraphemeCount, wordSeparatorGraphemeCount, wordSeparatorCharLength);

            _ = lineStringBuilder.Append(owocrWord.Text);
            if (owocrWord.Separator is not null)
            {
                _ = lineStringBuilder.Append(owocrWord.Separator);
            }
        }

        return new Line(new BoundingBox(owocrLine.BoundingBox, imageProperties), words, lineStringBuilder.ToString(), (owocrLine.WritingDirection ?? paragraphWritingDirection).ToWritingDirection());
    }
}
