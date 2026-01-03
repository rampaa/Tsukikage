using System.Diagnostics;
using System.Globalization;

namespace Tsukikage.Utilities;

internal static class ExtensionMethods
{
    public static void SafeFireAndForget(this Task task, string errorMessage)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        _ = task.ContinueWith(
            static (t, state) =>
            {
                string? message = (string?)state;
                Debug.Assert(message is not null);
                Console.Error.WriteLine($"{message}\n{t.Exception}");
            },
            errorMessage,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public static int GetGraphemeCount(this ReadOnlySpan<char> text)
    {
        int count = 0;

        int index = 0;
        while (index < text.Length)
        {
            int length = StringInfo.GetNextTextElementLength(text[index..]);
            index += length;

            ++count;
        }

        return count;
    }

    public static GraphemeEnumerator EnumerateGraphemes(this ReadOnlySpan<char> text)
    {
        return new GraphemeEnumerator(text);
    }
}
