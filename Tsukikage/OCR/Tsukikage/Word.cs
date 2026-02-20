using Tsukikage.Interop;
using Tsukikage.Utilities;

namespace Tsukikage.OCR.Tsukikage;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed record class Word(string Text, in BoundingBox BoundingBox)
{
    public BoundingBox BoundingBox { get; } = BoundingBox;
    public string Text { get; set; } = Text;

    public int GetGraphemeIndexFromPosition(Point mousePosition, int graphemeCount, char firstChar, char lastChar, WritingDirection writingDirection)
    {
        float offsetFromCenterX = mousePosition.X - BoundingBox.CenterX;
        float offsetFromCenterY = mousePosition.Y - BoundingBox.CenterY;

        float normalizedOffset;
        bool horizontal = writingDirection is WritingDirection.LeftToRightTopToBottom or WritingDirection.Ambiguous;

        if (horizontal)
        {
            float localX = BoundingBox.SinNegativeRotation is 0f
                ? offsetFromCenterX
                : (offsetFromCenterX * BoundingBox.CosNegativeRotation) - (offsetFromCenterY * BoundingBox.SinNegativeRotation);

            normalizedOffset = (localX + BoundingBox.HalfWidth) * BoundingBox.WidthReciprocal;
        }
        else
        {
            float localY = BoundingBox.SinNegativeRotation is 0f
                ? offsetFromCenterY
                : (offsetFromCenterX * BoundingBox.SinNegativeRotation) + (offsetFromCenterY * BoundingBox.CosNegativeRotation);

            normalizedOffset = (localY + BoundingBox.HalfHeight) * BoundingBox.HeightReciprocal;
        }

        float cell = 1f / graphemeCount;
        float leading = JapaneseUtils.LeftBrackets.Contains(firstChar) ? 0.5f : 0f;
        float trailing = JapaneseUtils.RightBrackets.Contains(lastChar) ? 0.5f : 0f;
        normalizedOffset = (normalizedOffset + (cell * leading)) / (1f + (cell * (leading + trailing)));

        int index = float.ConvertToIntegerNative<int>(normalizedOffset * graphemeCount);
        return Math.Clamp(index, 0, graphemeCount - 1);
    }
}
