using Tsukikage.Interop;
using Tsukikage.Utilities.Bool;

namespace Tsukikage.OCR;

internal static class MouseMoveWorker
{
    private static Point s_mousePosition;

    private static readonly Lock s_pointLock = new();
    private static readonly AtomicBool s_pending = new(false);
    private static readonly AutoResetEvent s_autoResetEvent = new(false);

    static MouseMoveWorker()
    {
        new Thread(Worker)
        {
            IsBackground = true
        }.Start();
    }

    public static void Signal(Point point)
    {
        lock (s_pointLock)
        {
            s_mousePosition = point;
        }

        if (s_pending.TrySetTrue())
        {
            _ = s_autoResetEvent.Set();
        }
    }

    private static void Worker()
    {
        while (true)
        {
            _ = s_autoResetEvent.WaitOne();


            while (s_pending.TrySetFalse())
            {
                Point currentPoint;
                lock (s_pointLock)
                {
                    currentPoint = s_mousePosition;
                }

                OcrUtils.HandleMouseMove(currentPoint);
            }
        }
    }
}
