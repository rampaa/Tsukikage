using System.Diagnostics;

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
}
