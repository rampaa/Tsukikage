using Tsukikage.Interop;

namespace Tsukikage.OCR;

internal static class MouseMoveWorker
{
    private static Point s_mousePosition;

    private static int s_pending;     // 0 = no new input, 1 = new input
    private static int s_processing;  // 0 = idle, 1 = OCR running

    private static readonly AutoResetEvent s_autoResetEvent = new(false);

    static MouseMoveWorker()
    {
        Thread thread = new(Worker)
        {
            IsBackground = true
        };

        thread.Start();
    }

    public static void Signal(Point point)
    {
        s_mousePosition = point;
        Volatile.Write(ref s_pending, 1);
        if (Volatile.Read(ref s_processing) is 0)
        {
            _ = s_autoResetEvent.Set();
        }
    }

    // ReSharper disable once FunctionNeverReturns
    private static void Worker()
    {
        while (true)
        {
            _ = s_autoResetEvent.WaitOne();

            if (Interlocked.Exchange(ref s_processing, 1) is 1)
            {
                continue;
            }

            do
            {
                Volatile.Write(ref s_pending, 0);
                OcrUtils.HandleMouseMove(s_mousePosition);
            }
            while (Volatile.Read(ref s_pending) is 1);

            Volatile.Write(ref s_processing, 0);
        }
    }
}
