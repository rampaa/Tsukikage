using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tsukikage.Interop;
using Tsukikage.OCR.OwOCR;

namespace Tsukikage.OCR.Tsukikage;

internal readonly record struct BoundingBox
{
    public float CenterX { get; }
    public float CenterY { get; }
    public float HalfWidth { get; }
    public float HalfHeight { get; }
    public float CosNegativeRotation { get; }
    public float SinNegativeRotation { get; }
    public float WidthReciprocal { get; }
    public float HeightReciprocal { get; }

    public BoundingBox(OwocrBoundingBox boundingBox, in OwocrImageProperties imageProperties)
    {
        float imageWidth = boundingBox.Width * imageProperties.Width;
        float imageHeight = boundingBox.Height * imageProperties.Height;
        Debug.Assert(imageWidth > 0 && imageHeight > 0);

        HalfWidth = imageWidth * 0.5f;
        HalfHeight = imageHeight * 0.5f;

        Debug.Assert(imageProperties.X is not null && imageProperties.Y is not null);
        CenterX = imageProperties.X.Value + (boundingBox.CenterX * imageProperties.Width);
        CenterY = imageProperties.Y.Value + (boundingBox.CenterY * imageProperties.Height);

        float negativeRotationZ = -boundingBox.RotationZ ?? 0;
        CosNegativeRotation = MathF.Cos(negativeRotationZ);
        SinNegativeRotation = MathF.Sin(negativeRotationZ);

        WidthReciprocal = 1.0f / imageWidth;
        HeightReciprocal = 1.0f / imageHeight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseOver(Point mousePosition)
    {
        float offsetFromCenterX = mousePosition.X - CenterX;
        float offsetFromCenterY = mousePosition.Y - CenterY;

        if (SinNegativeRotation is 0f)
        {
            return MathF.Abs(offsetFromCenterX) <= HalfWidth && MathF.Abs(offsetFromCenterY) <= HalfHeight;
        }

        float rotatedOffsetX = (offsetFromCenterX * CosNegativeRotation) - (offsetFromCenterY * SinNegativeRotation);
        float rotatedOffsetY = (offsetFromCenterX * SinNegativeRotation) + (offsetFromCenterY * CosNegativeRotation);

        return MathF.Abs(rotatedOffsetX) <= HalfWidth && MathF.Abs(rotatedOffsetY) <= HalfHeight;
    }
}
