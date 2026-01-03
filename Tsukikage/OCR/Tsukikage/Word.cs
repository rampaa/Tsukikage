using System.Runtime.CompilerServices;
using Tsukikage.Interop;

namespace Tsukikage.OCR.Tsukikage;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed record class Word(string Text, in BoundingBox BoundingBox)
{
    public BoundingBox BoundingBox { get; } = BoundingBox;
    public string Text { get; set; } = Text;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetGraphemeIndexFromPosition(Point mousePosition, int graphemeCount, WritingDirection? writingDirection)
    {
        float offsetFromCenterX = mousePosition.X - BoundingBox.CenterX;
        float offsetFromCenterY = mousePosition.Y - BoundingBox.CenterY;

        float localX;
        float localY;
        if (BoundingBox.SinNegativeRotation is 0f)
        {
            localX = offsetFromCenterX;
            localY = offsetFromCenterY;
        }
        else
        {
            localX = (offsetFromCenterX * BoundingBox.CosNegativeRotation) - (offsetFromCenterY * BoundingBox.SinNegativeRotation);
            localY = (offsetFromCenterX * BoundingBox.SinNegativeRotation) + (offsetFromCenterY * BoundingBox.CosNegativeRotation);
        }

        float normalizedOffset;
        if (writingDirection is WritingDirection.LeftToRightTopToBottom or WritingDirection.Ambiguous)
        {
            normalizedOffset = (localX + BoundingBox.HalfWidth) * BoundingBox.WidthReciprocal;
        }
        else
        {
            normalizedOffset = (localY + BoundingBox.HalfHeight) * BoundingBox.HeightReciprocal;
        }

        int index = float.ConvertToIntegerNative<int>(normalizedOffset * graphemeCount);
        return index < 0
            ? 0
            : index >= graphemeCount
                ? graphemeCount - 1
                : index;
    }
}
