using Tsukikage.Utilities;

namespace Tsukikage.Tests;

internal sealed class TextCorrectionUtilsTests
{
    [TestCase("愛の証", "愛の証", "愛の証")]
    [TestCase("愛の証", "愛の振", "愛の証")]
    [TestCase("愛の証", "愛　の　振", "愛　の　証")]
    [TestCase("愛の証", "愛の振…", "愛の証…")]
    [TestCase("…愛の証…", "愛の振", "愛の証")]
    [TestCase("愛の、証", "愛の振", "愛の証")]
    public void TryReplaceOcrTextWithTextHookerText_ShouldReturnTrue_WhenTextsAreNearlyIdentical(string normalizedTextHookerText, string ocrText, string expected)
    {
        // Act
        bool success = TextCorrectionUtils.TryReplaceOcrTextWithTextHookerText(normalizedTextHookerText, ocrText, out string? resultText);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(resultText, Is.EqualTo(expected));
        }
    }

    [TestCase("証振", "振証", null)]
    [TestCase("愛の証", "愛ののののの振", null)]
    [TestCase("愛の証", "愛振", null)]
    public void TryReplaceOcrTextWithTextHookerText_ShouldReturnTrue_WhenTextsAreDifferent(string normalizedTextHookerText, string ocrText, string? expected)
    {
        // Act
        bool success = TextCorrectionUtils.TryReplaceOcrTextWithTextHookerText(normalizedTextHookerText, ocrText, out string? resultText);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(resultText, Is.EqualTo(expected));
        }
    }
}
