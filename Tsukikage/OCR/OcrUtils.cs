using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Tsukikage.Interop;
using Tsukikage.OCR.OwOCR;
using Tsukikage.OCR.Tsukikage;
using Tsukikage.Utilities;
using Tsukikage.Utilities.Json;
using Tsukikage.Websocket;

namespace Tsukikage.OCR;

internal static class OcrUtils
{
    private static OcrResult? s_ocrResult;

    private static readonly LinkedList<string> s_textHookerTextBacklog = [];

    private static bool s_mouseWasOverWordBoundingBox; // = false

    private static bool s_textHookerTextChanged; // = false
    private static bool s_ocrTextChanged; // = false

    public static void ProcessWebSocketText(string text, bool isTextFromTextHooker)
    {
        OcrResult? ocrResult;
        LinkedListNode<string>? textHookerTextNode;
        if (!isTextFromTextHooker)
        {
            ocrResult = text.Length is not 0
                ? GetOcrResult(text)
                : null;

            s_ocrResult = ocrResult;
            textHookerTextNode = s_textHookerTextBacklog.First;
            s_ocrTextChanged = true;
        }
        else
        {
            if (s_textHookerTextBacklog.Count is 10)
            {
                s_textHookerTextBacklog.RemoveLast();
            }

            textHookerTextNode = s_textHookerTextBacklog.AddFirst(text.Trim());
            ocrResult = s_ocrResult;
            s_textHookerTextChanged = true;
        }

        if (textHookerTextNode is null || ocrResult is null)
        {
            return;
        }

        if (!s_ocrTextChanged || !s_textHookerTextChanged)
        {
            return;
        }

        int maxParagraphTextLength = 0;
        int minParagraphTextLength = int.MaxValue;

        Debug.Assert(ocrResult.Paragraphs.Length > 0);
        foreach (Paragraph paragraph in ocrResult.Paragraphs)
        {
            int paragraphTextLength = paragraph.Text.Length;
            if (paragraphTextLength > maxParagraphTextLength)
            {
                maxParagraphTextLength = paragraphTextLength;
            }
            if (paragraphTextLength < minParagraphTextLength)
            {
                minParagraphTextLength = paragraphTextLength;
            }
        }

        string textHookerText = "";
        while (textHookerTextNode is not null)
        {
            textHookerText = textHookerTextNode.Value + textHookerText;
            int textHookerTextLength = textHookerText.Length;
            if (textHookerTextLength / 2f > maxParagraphTextLength
                || minParagraphTextLength / 2f > textHookerTextLength)
            {
                break;
            }

            Paragraph[] paragraphs = ocrResult.Paragraphs;
            foreach (Paragraph paragraph in paragraphs)
            {
                string paragraphText = paragraph.Text;
                int paragraphTextLength = paragraphText.Length;
                if (textHookerTextLength / 2f > paragraphTextLength
                    || paragraphTextLength / 2f > textHookerTextLength)
                {
                    continue;
                }

                if (paragraphText == textHookerText)
                {
                    while (textHookerTextNode.Previous is not null)
                    {
                        s_textHookerTextBacklog.Remove(textHookerTextNode.Previous);
                    }

                    s_textHookerTextChanged = false;
                    s_ocrTextChanged = false;

                    Console.WriteLine("Same text.");
                    return;
                }

                if (TryReplaceOcrTextWithTextHookerText(textHookerText, paragraphText, out string? resultText))
                {
                    Console.WriteLine("Replaced OCR text with TextHooker text.");
                    //Console.WriteLine($"OCR text:\n{paragraphText}");
                    //Console.WriteLine($"Text hooker text:\n{resultText}\n");

                    paragraph.Text = resultText;
                    RebuildOcrResult(paragraph);

                    while (textHookerTextNode.Previous is not null)
                    {
                        s_textHookerTextBacklog.Remove(textHookerTextNode.Previous);
                    }

                    s_textHookerTextChanged = false;
                    s_ocrTextChanged = false;

                    return;
                }
            }

            textHookerTextNode = textHookerTextNode.Next;
        }

        if (s_textHookerTextBacklog.First is not null && !s_mouseWasOverWordBoundingBox)
        {
            Console.WriteLine($"Failed to replace OCR text with TextHooker text.\nCurrent text hooker text:\n{s_textHookerTextBacklog.First.Value}\n");
        }
    }

    private static void RebuildOcrResult(Paragraph paragraph)
    {
        int currentIndex = 0;

        string newParagraphText = paragraph.Text;
        for (int i = 0; i < paragraph.Lines.Length; i++)
        {
            Line line = paragraph.Lines[i];
            int lineStartIndex = currentIndex;

            for (int j = 0; j < line.Words.Length; j++)
            {
                Word word = line.Words[j];
                int wordLength = word.Text.Length;

                word.Text = newParagraphText[currentIndex..(currentIndex + wordLength)];
                currentIndex += wordLength;
            }

            line.Text = newParagraphText[lineStartIndex..currentIndex];
        }
    }


    private static OcrResult? GetOcrResult(string text)
    {
        try
        {
            OwocrOcrResult? owocrOcrResult = text.StartsWith('{')
                ? JsonSerializer.Deserialize(text, OcrResultJsonContext.Default.OwocrOcrResult)
                : null;

            return owocrOcrResult?.ToOcrResult();
        }
        catch (JsonException ex)
        {
            Debug.WriteLine("GetOcrResult failed");
            Debug.WriteLine(ex.Message);
            return null;
        }
    }

    public static void HandleMouseMove(Point mousePosition)
    {
        OcrResult? ocrResult = s_ocrResult;
        if (ocrResult is null)
        {
            return;
        }

        Rectangle imageProperties = ocrResult.ImageProperties;
        mousePosition = MagpieUtils.GetMousePosition(mousePosition);
        bool mouseIsNotOverAnyLines = true;
        if (imageProperties.Contains(mousePosition))
        {
            for (int paragraphIndex = 0; paragraphIndex < ocrResult.Paragraphs.Length; paragraphIndex++)
            {
                Paragraph paragraph = ocrResult.Paragraphs[paragraphIndex];
                if (!paragraph.BoundingBox.IsMouseOver(mousePosition))
                {
                    continue;
                }

                Line[] lines = paragraph.Lines;
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    Line line = lines[lineIndex];
                    if (!line.BoundingBox.IsMouseOver(mousePosition))
                    {
                        continue;
                    }

                    mouseIsNotOverAnyLines = false;
                    Word[] words = line.Words;
                    for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
                    {
                        Word word = words[wordIndex];
                        if (!word.BoundingBox.IsMouseOver(mousePosition))
                        {
                            continue;
                        }

                        string output;
                        if (ConfigManager.OutputType is OutputPayload.GraphemeInfo or OutputPayload.TextStartingFromPosition)
                        {
                            int charStartIndex = 0;
                            for (int prevLineIndex = 0; prevLineIndex < lineIndex; prevLineIndex++)
                            {
                                Line prevLine = lines[prevLineIndex];
                                charStartIndex += prevLine.Text.Length;
                            }

                            for (int prevWordIndex = 0; prevWordIndex < wordIndex; prevWordIndex++)
                            {
                                Word prevWord = words[prevWordIndex];
                                charStartIndex += prevWord.Text.Length;
                            }

                            if (word.Text.Length > 1)
                            {
                                int graphemeCount = word.Text.GetGraphemeCount();
                                if (graphemeCount > 1)
                                {
                                    charStartIndex += word.GetGraphemeIndexFromPosition(mousePosition, graphemeCount, paragraph.WritingDirection);
                                }
                            }

                            if (ConfigManager.OutputType is OutputPayload.GraphemeInfo)
                            {
                                GraphemeInfo graphemeInfo = new(charStartIndex, paragraph.Text, paragraph.WritingDirection is WritingDirection.TopToBottomRightToLeft);
                                output = JsonSerializer.Serialize(graphemeInfo, OcrResultJsonContext.Default.GraphemeInfo);
                            }
                            else // if (ConfigManager.OutputType is OutputType.TextStartingFromPosition)
                            {
                                output = paragraph.Text[charStartIndex..];
                            }
                        }
                        else if (ConfigManager.OutputType is OutputPayload.Paragraph)
                        {
                            output = paragraph.Text;
                        }
                        else if (ConfigManager.OutputType is OutputPayload.Line)
                        {
                            output = line.Text;
                        }
                        else if (ConfigManager.OutputType is OutputPayload.Word)
                        {
                            output = word.Text;
                        }
                        else // if (ConfigManager.OutputType is OutputPayload.Grapheme)
                        {
                            output = word.Text;
                            if (word.Text.Length > 1)
                            {
                                int graphemeCount = word.Text.GetGraphemeCount();
                                if (graphemeCount > 1)
                                {
                                    int graphemeIndex = word.GetGraphemeIndexFromPosition(mousePosition, graphemeCount, paragraph.WritingDirection);
                                    output = StringInfo.GetNextTextElement(word.Text, graphemeIndex);
                                }
                            }
                        }

                        if (ConfigManager.OutputIpcMethod is OutputIpcMethod.WebSocket)
                        {
                            WebsocketServerUtils.Broadcast(output);
                        }
                        else // if (ConfigManager.OutputIpcMethod is OutputIpcMethod.Clipboard)
                        {
                            WinApi.SetClipboardText(output);
                        }

                        s_mouseWasOverWordBoundingBox = true;
                        return;
                    }
                }
            }
        }

        if (mouseIsNotOverAnyLines && s_mouseWasOverWordBoundingBox)
        {
            s_mouseWasOverWordBoundingBox = false;
            if (ConfigManager.OutputIpcMethod is OutputIpcMethod.WebSocket)
            {
                WebsocketServerUtils.Broadcast("");
            }
            else // if (ConfigManager.OutputIpcMethod is OutputIpcMethod.Clipboard)
            {
                WinApi.SetClipboardText("");
            }
        }
    }

    private static bool TryReplaceOcrTextWithTextHookerText(string textFromTextHooker, string textFromOcr, [NotNullWhen(true)] out string? resultText)
    {
        if (textFromTextHooker.Length is 0 || textFromOcr.Length is 0)
        {
            resultText = null;
            return false;
        }

        string normalizedTextFromOcr = textFromOcr.IsNormalized(NormalizationForm.FormC)
            ? textFromOcr
            : textFromOcr.Normalize(NormalizationForm.FormC);

        if (normalizedTextFromOcr.Length != textFromOcr.Length)
        {
            resultText = null;
            return false;
        }

        string normalizedTextFromTextHooker = textFromTextHooker.IsNormalized(NormalizationForm.FormC)
            ? textFromTextHooker
            : textFromTextHooker.Normalize(NormalizationForm.FormC);

        const float similarityThreshold = 0.7f;
        const float hardMismatchPenalty = 1.0f;
        const float softMismatchPenalty = 0.4f;
        // const float spaceMismatchPenalty = 0.3f;

        float mismatchPenalty = 0f;
        int ocrRuneCount = 0;
        GraphemeEnumerator normalizedTextFromTextHookerEnumerator = normalizedTextFromTextHooker.EnumerateGraphemes();
        GraphemeEnumerator normalizedTextFromOcrEnumerator = normalizedTextFromOcr.EnumerateGraphemes();

        StringBuilder finalText = new(textFromOcr.Length);

        bool moveNormalizedTextFromTextHookerEnumerator = true;
        int normalizedTextFromTextHookerLength = normalizedTextFromTextHooker.Length;
        while (true)
        {
            bool hasTextHookerRune;
            if (moveNormalizedTextFromTextHookerEnumerator)
            {
                hasTextHookerRune = normalizedTextFromTextHookerEnumerator.MoveNext();
            }
            else
            {
                hasTextHookerRune = true;
                moveNormalizedTextFromTextHookerEnumerator = true;
            }

            bool hasOcrRune = normalizedTextFromOcrEnumerator.MoveNext();
            if (hasTextHookerRune != hasOcrRune)
            {
                resultText = null;
                return false;
            }

            if (!hasTextHookerRune)
            {
                break;
            }

            ReadOnlySpan<char> textHookerRune = normalizedTextFromTextHookerEnumerator.Current;
            ReadOnlySpan<char> ocrRune = normalizedTextFromOcrEnumerator.Current;
            ++ocrRuneCount;

            if (textHookerRune == ocrRune)
            {
                _ = finalText.Append(textHookerRune);
                continue;
            }

            if (textHookerRune.Length > 1 || ocrRune.Length > 1)
            {
                _ = finalText.Append(textHookerRune);
                mismatchPenalty += hardMismatchPenalty;
            }
            else
            {
                char textHookerChar = textHookerRune[0];
                char ocrChar = ocrRune[0];

                char furtherNormalizedTextHookerChar = textHookerChar;
                char furtherNormalizedOcrChar = ocrChar;
                if (JapaneseUtils.NormalizationDict.TryGetValue(textHookerChar, out char mappedChar))
                {
                    furtherNormalizedTextHookerChar = mappedChar;
                }

                if (JapaneseUtils.NormalizationDict.TryGetValue(ocrChar, out mappedChar))
                {
                    furtherNormalizedOcrChar = mappedChar;
                }

                if (furtherNormalizedTextHookerChar != furtherNormalizedOcrChar)
                {
                    if (JapaneseUtils.FrequentlyMisparsedCharactersDict.TryGetValue(furtherNormalizedTextHookerChar, out mappedChar))
                    {
                        furtherNormalizedTextHookerChar = mappedChar;
                    }

                    if (JapaneseUtils.FrequentlyMisparsedCharactersDict.TryGetValue(furtherNormalizedOcrChar, out mappedChar))
                    {
                        furtherNormalizedOcrChar = mappedChar;
                    }

                    mismatchPenalty += furtherNormalizedTextHookerChar == furtherNormalizedOcrChar
                        ? softMismatchPenalty
                        : hardMismatchPenalty;
                }

                _ = finalText.Append(textHookerChar);
            }

            if (1f - (mismatchPenalty / normalizedTextFromTextHookerLength) < similarityThreshold)
            {
                resultText = null;
                return false;
            }
        }

        float finalPenaltyResult = 1f - (mismatchPenalty / ocrRuneCount);
        bool result = finalPenaltyResult >= similarityThreshold;
        resultText = result ? finalText.ToString() : null;
        return result;
    }
}
