using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Tsukikage.Utilities;

internal static class TextCorrectionUtils
{
#pragma warning disable CA1028 // Enum Storage should be Int32
    private enum AlignmentDirection : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        None = 0,
        BacktraceSkipTextHookerGrapheme = 1,
        BacktraceKeepOcrGrapheme = 2,
        BacktraceMatchingGraphemes = 3
    }

    private const float BudgetCoefficient = 6.5f;
    private const float BudgetExponent = 0.80f;

    private const int SkipTextHookerPunctuationPenalty = 4;
    private const int SkipOcrWhitespacePenalty = 6;
    private const int SkipOcrAtEdgePenalty = 10;

    private const int NormalizedMatchPenalty = 1;
    private const int MisparsedCharacterMatchPenalty = 4;
    private const int FullSubstitutionPenalty = 10;

    // Hard cap on how many OCR graphemes may be skipped at the leading/trailing edge regardless of the penalty budget.
    // Prevents implausibly long hallucinations from matching even when the budget would technically permit them.
    private const int MaxLeadingEdgeSkips = 10;
    private const int MaxTrailingEdgeSkips = 10;

    private static readonly int s_minimumSkipPenalty = Math.Max(1, Math.Min(SkipTextHookerPunctuationPenalty, Math.Min(SkipOcrAtEdgePenalty, SkipOcrWhitespacePenalty)));

    // Handles the following patterns:
    // 1. Random OCR recognition errors:
    //    normalizedTextHookerText: "愛の証", ocrText: "愛の振", resultText: "愛の証"
    // 2. OCR results containing extra spaces compared to the TextHooker text:
    //    normalizedTextHookerText: "愛の証", ocrText: "愛　の　振", resultText is "愛　の　証"
    // 3. OCR results hallucinating leading or trailing characters that are not present in the TextHooker text. At most MaxLeadingEdgeSkips/MaxTrailingEdgeSkips OCR graphemes may be skipped at each edge regardless of budget.
    //    normalizedTextHookerText: "愛の証", ocrText: "愛の振…", resultText: "愛の証…". 
    // 4. OCR results missing leading or trailing punctuation/whitespace characters that are present in the TextHooker text:
    //    normalizedTextHookerText: "…愛の証…", ocrText: "愛の振", resultText: "愛の証".
    // 5. OCR result missing punctuation/whitespace characters in the middle of the text:
    //    normalizedTextHookerText: "愛の、証", ocrText: "愛の振", resultText: "愛の証".
    // Extra characters hallucinated in the middle of the OCRed text (excluding whitespace, see the 2nd case) cannot be handled safely, so they are intentionally not supported.
    // Missing characters in the middle of the OCRed text (excluding punctuation/whitespace, see the 5th case) cannot be handled safely either, so they are also intentionally not supported.
    public static bool TryReplaceOcrTextWithTextHookerText(string normalizedTextHookerText, string ocrText, [NotNullWhen(true)] out string? resultText)
    {
        resultText = null;

        string normalizedOcrText = ocrText.IsNormalized(NormalizationForm.FormC)
            ? ocrText
            : ocrText.Normalize(NormalizationForm.FormC);

        int[] ocrGraphemeBoundaries = ArrayPool<int>.Shared.Rent(normalizedOcrText.Length + 1);
        int[] textHookerGraphemeBoundaries = ArrayPool<int>.Shared.Rent(normalizedTextHookerText.Length + 1);

        int[]? penaltyTable = null;
        AlignmentDirection[]? backtraceTable = null;
        char[]? bestResultBuffer = null;
        char[]? comparisonBuffer = null;

        try
        {
            int ocrTextGraphemeCount = FillGraphemeBoundaries(normalizedOcrText, ocrGraphemeBoundaries);
            int textHookerTextGraphemeCount = FillGraphemeBoundaries(normalizedTextHookerText, textHookerGraphemeBoundaries);

            int maxAllowedPenalty = (int)MathF.Round(BudgetCoefficient * MathF.Pow(ocrTextGraphemeCount, BudgetExponent));

            int textHookerTextContentStartIndex = 0;
            while (textHookerTextContentStartIndex < textHookerTextGraphemeCount && IsPunctuationOrWhitespace(normalizedTextHookerText, textHookerGraphemeBoundaries[textHookerTextContentStartIndex]))
            {
                ++textHookerTextContentStartIndex;
            }

            int textHookerTextContentEndIndex = textHookerTextGraphemeCount - 1;
            while (textHookerTextContentEndIndex >= textHookerTextContentStartIndex && IsPunctuationOrWhitespace(normalizedTextHookerText, textHookerGraphemeBoundaries[textHookerTextContentEndIndex]))
            {
                --textHookerTextContentEndIndex;
            }

            int textHookerTextVariantCapacity = 0;
            bool textHookerTextStartsWithContent = textHookerTextContentStartIndex is 0;
            bool textHookerTextEndsWithContent = textHookerTextContentEndIndex == textHookerTextGraphemeCount - 1;
            if (textHookerTextStartsWithContent && textHookerTextEndsWithContent)
            {
                textHookerTextVariantCapacity = 1;
            }
            else if (!textHookerTextStartsWithContent && !textHookerTextEndsWithContent)
            {
                textHookerTextVariantCapacity = 4;
            }
            else
            {
                textHookerTextVariantCapacity = 2;
            }

            Span<(int StartIndex, int EndIndex)> textHookerTextVariantRanges = stackalloc (int, int)[textHookerTextVariantCapacity];
            textHookerTextVariantRanges[0] = (0, textHookerTextGraphemeCount - 1);
            if (textHookerTextVariantCapacity is 4)
            {
                textHookerTextVariantRanges[1] = (textHookerTextContentStartIndex, textHookerTextContentEndIndex);
                textHookerTextVariantRanges[2] = (textHookerTextContentStartIndex, textHookerTextGraphemeCount - 1);
                textHookerTextVariantRanges[3] = (0, textHookerTextContentEndIndex);
            }
            else if (textHookerTextVariantCapacity is 2)
            {
                textHookerTextVariantRanges[1] = textHookerTextStartsWithContent
                    ? (0, textHookerTextContentEndIndex)
                    : (textHookerTextContentStartIndex, textHookerTextGraphemeCount - 1);
            }

            int maxCellCount = (ocrTextGraphemeCount + 1) * (textHookerTextGraphemeCount + 1);
            penaltyTable = ArrayPool<int>.Shared.Rent(maxCellCount);
            backtraceTable = ArrayPool<AlignmentDirection>.Shared.Rent(maxCellCount);

            int maxChars = normalizedOcrText.Length + normalizedTextHookerText.Length;
            bestResultBuffer = ArrayPool<char>.Shared.Rent(maxChars);
            comparisonBuffer = ArrayPool<char>.Shared.Rent(maxChars);

            int lowestPenaltyScore = int.MaxValue;
            int bestResultStartIndex = 0;
            int bestResultLength = 0;
            bool isResultAmbiguous = false;
            int bandRadius = maxAllowedPenalty / s_minimumSkipPenalty;

            // The first OCR row index at which trailing edge skips become permitted.
            // Negative values mean all rows are eligible
            int trailingSkipStartIndex = ocrTextGraphemeCount - MaxTrailingEdgeSkips;

            for (int i = 0; i < textHookerTextVariantRanges.Length; i++)
            {
                (int textHookerTextStartIndex, int textHookerTexEndIndex) = textHookerTextVariantRanges[i];
                int textHookerTextLength = textHookerTexEndIndex - textHookerTextStartIndex + 1;
                int tableRowStride = textHookerTextLength + 1;
                int cellsToClear = (ocrTextGraphemeCount + 1) * tableRowStride;

                Array.Fill(penaltyTable, int.MaxValue, 0, cellsToClear);
                Array.Clear(backtraceTable, 0, cellsToClear);
                penaltyTable[0] = 0;

                for (int j = 0; j <= ocrTextGraphemeCount; j++)
                {
                    int currentRowOffset = j * tableRowStride;
                    int nextRowOffset = currentRowOffset + tableRowStride;
                    int minK = Math.Max(0, j - bandRadius);
                    int maxK = Math.Min(textHookerTextLength, j + bandRadius);

                    for (int k = minK; k <= maxK; k++)
                    {
                        int currentPenalty = penaltyTable[currentRowOffset + k];
                        if (currentPenalty > maxAllowedPenalty)
                        {
                            continue;
                        }

                        // Move 1: Skip TextHooker grapheme (horizontal)
                        if (k < maxK && IsPunctuationOrWhitespace(normalizedTextHookerText, textHookerGraphemeBoundaries[textHookerTextStartIndex + k]))
                        {
                            int penalty = currentPenalty + SkipTextHookerPunctuationPenalty;
                            int destination = currentRowOffset + k + 1;
                            if (penalty < penaltyTable[destination])
                            {
                                penaltyTable[destination] = penalty;
                                backtraceTable[destination] = AlignmentDirection.BacktraceSkipTextHookerGrapheme;
                            }
                        }

                        if (j < ocrTextGraphemeCount)
                        {
                            int ocrCharacterIndex = ocrGraphemeBoundaries[j];
                            bool isWhitespaceOnly = char.IsWhiteSpace(normalizedOcrText[ocrCharacterIndex]);

                            bool isAtLeadingEdge = (k is 0) && (j < MaxLeadingEdgeSkips);
                            bool isAtTrailingEdge = (k == textHookerTextLength) && (j >= trailingSkipStartIndex);

                            // Move 2: Skip OCR grapheme (vertical)
                            if (isAtLeadingEdge || isAtTrailingEdge || isWhitespaceOnly)
                            {
                                int penalty = currentPenalty + (isWhitespaceOnly ? SkipOcrWhitespacePenalty : SkipOcrAtEdgePenalty);
                                int destination = nextRowOffset + k;
                                if (penalty < penaltyTable[destination])
                                {
                                    penaltyTable[destination] = penalty;
                                    backtraceTable[destination] = AlignmentDirection.BacktraceKeepOcrGrapheme;
                                }
                            }

                            // Move 3: Match/Substitution (diagonal)
                            if (k < textHookerTextLength)
                            {
                                int hookerCharacterIndex = textHookerGraphemeBoundaries[textHookerTextStartIndex + k];
                                int alignmentPenalty = CalculateGraphemeMatchPenalty(
                                    normalizedOcrText, ocrCharacterIndex, ocrGraphemeBoundaries[j + 1] - ocrCharacterIndex,
                                    normalizedTextHookerText, hookerCharacterIndex, textHookerGraphemeBoundaries[textHookerTextStartIndex + k + 1] - hookerCharacterIndex);

                                int totalDiagonalPenalty = currentPenalty + alignmentPenalty;
                                int dest = nextRowOffset + k + 1;
                                if (totalDiagonalPenalty < penaltyTable[dest])
                                {
                                    penaltyTable[dest] = totalDiagonalPenalty;
                                    backtraceTable[dest] = AlignmentDirection.BacktraceMatchingGraphemes;
                                }
                            }
                        }
                    }
                }

                int lastRowOffset = ocrTextGraphemeCount * tableRowStride;

                int terminalMinJ = Math.Max(0, ocrTextGraphemeCount - bandRadius);
                int terminalMaxJ = Math.Min(textHookerTextLength, ocrTextGraphemeCount + bandRadius);
                for (int j = terminalMinJ; j <= terminalMaxJ; j++)
                {
                    int finalPenalty = penaltyTable[lastRowOffset + j];
                    if (finalPenalty > maxAllowedPenalty)
                    {
                        continue;
                    }

                    int trailingCost = 0;
                    bool isValidEndState = true;
                    for (int k = j; k < textHookerTextLength; k++)
                    {
                        if (IsPunctuationOrWhitespace(normalizedTextHookerText, textHookerGraphemeBoundaries[textHookerTextStartIndex + k]))
                        {
                            trailingCost += SkipTextHookerPunctuationPenalty;
                        }
                        else
                        {
                            isValidEndState = false;
                            break;
                        }
                    }

                    if (!isValidEndState)
                    {
                        continue;
                    }

                    int totalPenalty = finalPenalty + trailingCost;
                    if (totalPenalty > maxAllowedPenalty)
                    {
                        continue;
                    }

                    if (totalPenalty < lowestPenaltyScore)
                    {
                        int writePos = ReconstructResult(
                            normalizedOcrText, ocrGraphemeBoundaries,
                            normalizedTextHookerText, textHookerGraphemeBoundaries,
                            textHookerTextStartIndex, backtraceTable, tableRowStride,
                            ocrTextGraphemeCount, j, bestResultBuffer);

                        lowestPenaltyScore = totalPenalty;
                        isResultAmbiguous = false;
                        bestResultStartIndex = writePos;
                        bestResultLength = bestResultBuffer.Length - writePos;
                    }
                    else if (totalPenalty == lowestPenaltyScore && !isResultAmbiguous)
                    {
                        int writePos = ReconstructResult(
                            normalizedOcrText, ocrGraphemeBoundaries,
                            normalizedTextHookerText, textHookerGraphemeBoundaries,
                            textHookerTextStartIndex, backtraceTable, tableRowStride,
                            ocrTextGraphemeCount, j, comparisonBuffer);

                        int compLength = comparisonBuffer.Length - writePos;
                        if (!comparisonBuffer.AsSpan(writePos, compLength).SequenceEqual(bestResultBuffer.AsSpan(bestResultStartIndex, bestResultLength)))
                        {
                            isResultAmbiguous = true;
                        }
                    }
                }
            }

            if (bestResultLength > 0 && !isResultAmbiguous)
            {
                resultText = new string(bestResultBuffer, bestResultStartIndex, bestResultLength);
                return true;
            }

            return false;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(ocrGraphemeBoundaries);
            ArrayPool<int>.Shared.Return(textHookerGraphemeBoundaries);

            if (penaltyTable != null)
            {
                ArrayPool<int>.Shared.Return(penaltyTable);
            }

            if (backtraceTable != null)
            {
                ArrayPool<AlignmentDirection>.Shared.Return(backtraceTable);
            }

            if (bestResultBuffer != null)
            {
                ArrayPool<char>.Shared.Return(bestResultBuffer);
            }

            if (comparisonBuffer != null)
            {
                ArrayPool<char>.Shared.Return(comparisonBuffer);
            }
        }
    }

    // Traces the backtrace table from (endingOcrIndex, endingHookerIndex) back to (0, 0) writing characters in reverse into the tail of characterBuffer.
    // Returns the write position: the result occupies characterBuffer[writePos..Length].
    private static int ReconstructResult(
        string ocrText, int[] ocrGraphemeBoundaries,
        string textHookerText, int[] textHookerGraphemeBoundaries,
        int variationStartOffset, AlignmentDirection[] backtraceTable, int tableRowStride,
        int ocrTextEndIndex, int textHookerTextEndIndex, Span<char> characterBuffer)
    {
        int writePosition = characterBuffer.Length;
        int ocrTextIndex = ocrTextEndIndex;
        int textHookerTextIndex = textHookerTextEndIndex;

        while (ocrTextIndex > 0 || textHookerTextIndex > 0)
        {
            switch (backtraceTable[(ocrTextIndex * tableRowStride) + textHookerTextIndex])
            {
                case AlignmentDirection.BacktraceSkipTextHookerGrapheme:
                {
                    --textHookerTextIndex;
                    break;
                }

                case AlignmentDirection.BacktraceKeepOcrGrapheme:
                {
                    --ocrTextIndex;

                    int start = ocrGraphemeBoundaries[ocrTextIndex];
                    int length = ocrGraphemeBoundaries[ocrTextIndex + 1] - start;
                    writePosition -= length;
                    ocrText.AsSpan(start, length).CopyTo(characterBuffer[writePosition..]);
                    break;
                }

                case AlignmentDirection.BacktraceMatchingGraphemes:
                {
                    --ocrTextIndex;
                    --textHookerTextIndex;

                    int start = textHookerGraphemeBoundaries[variationStartOffset + textHookerTextIndex];
                    int length = textHookerGraphemeBoundaries[variationStartOffset + textHookerTextIndex + 1] - start;
                    writePosition -= length;
                    textHookerText.AsSpan(start, length).CopyTo(characterBuffer[writePosition..]);
                    break;
                }

                case AlignmentDirection.None:
                default:
                {
                    Debug.Assert(false);
                    break;
                }
            }
        }

        return writePosition;
    }

    private static int FillGraphemeBoundaries(ReadOnlySpan<char> text, int[] graphemeBoundries)
    {
        int graphemeCount = 0;
        int graphemeIndex = 0;

        while (graphemeIndex < text.Length)
        {
            graphemeBoundries[graphemeCount] = graphemeIndex;
            ++graphemeCount;

            graphemeIndex += StringInfo.GetNextTextElementLength(text[graphemeIndex..]);
        }

        graphemeBoundries[graphemeCount] = text.Length;
        return graphemeCount;
    }

    private static bool IsPunctuationOrWhitespace(string text, int index)
    {
        char firstChar = text[index];
        if (!char.IsHighSurrogate(firstChar))
        {
            return char.IsPunctuation(firstChar) || char.IsWhiteSpace(firstChar);
        }

        Debug.Assert(index + 1 < text.Length);
        char secondChar = text[index + 1];
        Debug.Assert(char.IsLowSurrogate(secondChar));
        Rune rune = new(firstChar, secondChar);

        return Rune.IsPunctuation(rune) || Rune.IsWhiteSpace(rune);
    }

    private static int CalculateGraphemeMatchPenalty(string ocrText, int ocrGraphemeStartIndex, int ocrGraphemeLength, string textHookerText, int textHookerGraphemeStartIndex, int textHookerGraphemeLength)
    {
        if (ocrGraphemeLength > 1 || textHookerGraphemeLength > 1)
        {
            return (ocrGraphemeLength == textHookerGraphemeLength && ocrText.AsSpan(ocrGraphemeStartIndex, ocrGraphemeLength).SequenceEqual(textHookerText.AsSpan(textHookerGraphemeStartIndex, textHookerGraphemeLength)))
                    ? 0
                    : FullSubstitutionPenalty;
        }

        char ocrChar = ocrText[ocrGraphemeStartIndex];
        char textHookerChar = textHookerText[textHookerGraphemeStartIndex];
        if (ocrChar == textHookerChar)
        {
            return 0;
        }

        if (JapaneseUtils.NormalizationDict.TryGetValue(ocrChar, out char normalizedOcrChar))
        {
            ocrChar = normalizedOcrChar;
        }

        if (JapaneseUtils.NormalizationDict.TryGetValue(textHookerChar, out char normalizedTextHookerChar))
        {
            textHookerChar = normalizedTextHookerChar;
        }

        if (ocrChar == textHookerChar)
        {
            return NormalizedMatchPenalty;
        }

        if (JapaneseUtils.FrequentlyMisparsedCharactersDict.TryGetValue(ocrChar, out char misparsedOcrChar))
        {
            ocrChar = misparsedOcrChar;
        }

        if (JapaneseUtils.FrequentlyMisparsedCharactersDict.TryGetValue(textHookerChar, out char misparsedTextHookerChar))
        {
            textHookerChar = misparsedTextHookerChar;
        }

        return ocrChar == textHookerChar
            ? MisparsedCharacterMatchPenalty
            : FullSubstitutionPenalty;
    }
}
