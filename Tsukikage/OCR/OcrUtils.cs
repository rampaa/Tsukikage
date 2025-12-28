using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Tsukikage.Interop;
using Tsukikage.OCR.OwOCR;
using Tsukikage.Utilities;
using Tsukikage.Utilities.Json;
using Tsukikage.Websocket;

namespace Tsukikage.OCR;

internal static class OcrUtils
{
    private static OcrResult? s_ocrResult;

    private static readonly LinkedList<string> s_textHookerTextBacklog = [];

    private static bool s_lastTimeFoundText; // = false;

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
        }
        else
        {
            if (s_textHookerTextBacklog.Count is 10)
            {
                s_textHookerTextBacklog.RemoveLast();
            }

            textHookerTextNode = s_textHookerTextBacklog.AddFirst(text.Trim());
            ocrResult = s_ocrResult;
        }

        if (textHookerTextNode is null || ocrResult is null)
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

        bool foundExactMatch = false;
        bool replaced = false;
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
                    foundExactMatch = true;
                    break;
                }

                if (TryReplaceOcrTextWithTextHookerText(textHookerText, paragraphText, out string? resultText))
                {
                    Console.WriteLine("Replaced OCR text with TextHooker text.");
                    Console.WriteLine($"OCR text:\n{paragraphText}");
                    Console.WriteLine($"Text hooker text:\n{resultText}\n");

                    paragraph.Text = resultText;
                    replaced = true;

                    while (textHookerTextNode.Previous is not null)
                    {
                        s_textHookerTextBacklog.Remove(textHookerTextNode.Previous);
                    }

                    break;
                }
            }

            if (foundExactMatch || replaced)
            {
                break;
            }

            textHookerTextNode = textHookerTextNode.Next;
        }

        if (!replaced && s_textHookerTextBacklog.First is not null)
        {
            Console.WriteLine($"Failed to replace OCR text with TextHooker text.\nCurrent text hooker text:\n{s_textHookerTextBacklog.First.Value}\n");
        }
    }

    private static OcrResult? GetOcrResult(string text)
    {
        try
        {
            return text.StartsWith('{')
                ? JsonSerializer.Deserialize(text, OcrResultJsonContext.Default.OcrResult)
                : null;
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

        ImageProperties imageProperties = ocrResult.ImageProperties;
        mousePosition = MagpieUtils.GetMousePosition(mousePosition);
        bool mouseIsNotOverAnyLines = true;
        if (imageProperties.Contains(mousePosition))
        {
            for (int paragraphIndex = 0; paragraphIndex < ocrResult.Paragraphs.Length; paragraphIndex++)
            {
                Paragraph paragraph = ocrResult.Paragraphs[paragraphIndex];
                if (!IsMouseOver(mousePosition, imageProperties, paragraph.BoundingBox))
                {
                    continue;
                }

                Line[] lines = paragraph.Lines;
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    Line line = lines[lineIndex];
                    if (!IsMouseOver(mousePosition, imageProperties, line.BoundingBox))
                    {
                        continue;
                    }

                    mouseIsNotOverAnyLines = false;
                    Word[] words = line.Words;
                    for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
                    {
                        Word word = words[wordIndex];
                        if (!IsMouseOver(mousePosition, imageProperties, word.BoundingBox))
                        {
                            continue;
                        }

                        string output;
                        if (ConfigManager.OutputType is OutputPayload.WordInfo or OutputPayload.TextStartingFromPosition)
                        {
                            int wordStartIndex = 0;
                            for (int prevLineIndex = 0; prevLineIndex < lineIndex; prevLineIndex++)
                            {
                                Line prevLine = lines[prevLineIndex];
                                wordStartIndex += prevLine.Text.Length;
                            }

                            for (int prevWordIndex = 0; prevWordIndex < wordIndex; prevWordIndex++)
                            {
                                Word prevWord = words[prevWordIndex];
                                wordStartIndex += prevWord.Text.Length;
                            }

                            if (ConfigManager.OutputType is OutputPayload.WordInfo)
                            {
                                WordInfo wordInfo = new(wordStartIndex, paragraph.Text, paragraph.WritingDirection is WritingDirection.TOP_TO_BOTTOM);
                                output = JsonSerializer.Serialize(wordInfo, OcrResultJsonContext.Default.WordInfo);
                            }
                            else // if (ConfigManager.OutputType is OutputType.TextStartingFromPosition)
                            {
                                output = paragraph.Text[wordStartIndex..];
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
                        else // if (ConfigManager.OutputType is OutputType.Word)
                        {
                            output = word.Text;
                        }

                        if (ConfigManager.OutputIpcMethod is OutputIpcMethod.WebSocket)
                        {
                            WebsocketServerUtils.Broadcast(output);
                        }
                        else // if (ConfigManager.OutputIpcMethod is OutputIpcMethod.Clipboard)
                        {
                            WinApi.SetClipboardText(output);
                        }

                        s_lastTimeFoundText = true;
                        return;
                    }
                }
            }
        }

        if (mouseIsNotOverAnyLines && s_lastTimeFoundText)
        {
            s_lastTimeFoundText = false;
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

    private static bool IsMouseOver(Point mousePosition, ImageProperties imageProperties, BoundingBox boundingBox)
    {
        // Convert mouse to image-local coordinates
        double mouseXImage = mousePosition.X - imageProperties.X;
        double mouseYImage = mousePosition.Y - imageProperties.Y;

        // Precomputed pixel-space center & half-size
        float centerX = boundingBox.CenterX * imageProperties.Width;
        float centerY = boundingBox.CenterY * imageProperties.Height;
        float halfWidth = boundingBox.Width * imageProperties.Width * 0.5f;
        float halfHeight = boundingBox.Height * imageProperties.Height * 0.5f;

        // Vector from box center to mouse
        double dx = mouseXImage - centerX;
        double dy = mouseYImage - centerY;

        // Rotate point into box local space
        double localX = (dx * boundingBox.CosInverseRotation) - (dy * boundingBox.SinInverseRotation);
        double localY = (dx * boundingBox.SinInverseRotation) + (dy * boundingBox.CosInverseRotation);

        // Axis-aligned containment check
        return Math.Abs(localX) <= halfWidth && Math.Abs(localY) <= halfHeight;
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
        const float spaceMismatchPenalty = 0.3f;

        float mismatchPenalty = 0f;
        int ocrRuneCount = 0;
        StringRuneEnumerator normalizedTextFromTextHookerEnumerator = normalizedTextFromTextHooker.EnumerateRunes();
        StringRuneEnumerator normalizedTextFromOcrEnumerator = normalizedTextFromOcr.EnumerateRunes();

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

            Rune textHookerRune = normalizedTextFromTextHookerEnumerator.Current;
            Rune ocrRune = normalizedTextFromOcrEnumerator.Current;
            ++ocrRuneCount;

            if (textHookerRune.Value == ocrRune.Value)
            {
                _ = finalText.Append(textHookerRune);
                continue;
            }

            if (!textHookerRune.IsBmp || !ocrRune.IsBmp)
            {
                _ = finalText.Append(textHookerRune);
                mismatchPenalty += hardMismatchPenalty;
            }
            else
            {
                char textHookerChar = (char)textHookerRune.Value;
                char ocrChar = (char)ocrRune.Value;

                if (ocrChar is ' ' or '　' && textHookerChar is not ' ' and not '　')
                {
                    moveNormalizedTextFromTextHookerEnumerator = false;
                    _ = finalText.Append(ocrChar);
                    mismatchPenalty += spaceMismatchPenalty;
                }
                else
                {
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
