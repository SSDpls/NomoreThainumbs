using System.Threading;

namespace NoMoreThaiNums.App;

/// <summary>
/// Ensures only one app instance runs at a time using a named system mutex.
/// The mutex is held for the lifetime of the first instance; a second launch
/// detects it, optionally signals the first instance to show its window, and
/// exits immediately.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    public const string MutexName = "Local\\NoMoreThaiNums.SingleInstance";
    private const string ShowWindowEventName = "Local\\NoMoreThaiNums.ShowWindow";

    private Mutex? _mutex;
    private EventWaitHandle? _showWindowEvent;
    private bool _owned;

    /// <summary>
    /// Attempts to become the single running instance.
    /// Returns false when another instance already owns the mutex.
    /// </summary>
    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: false, MutexName);
        try
        {
            _owned = _mutex.WaitOne(TimeSpan.Zero, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            // A previous instance crashed while holding the mutex; the handle
            // is now owned by us and it is safe to continue.
            _owned = true;
        }

        if (_owned)
        {
            _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowWindowEventName);
        }

        return _owned;
    }

    /// <summary>
    /// Signals an already-running instance to show its settings window.
    /// Used by the second launch before it exits.
    /// </summary>
    public static void SignalExistingInstanceToShowWindow()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out EventWaitHandle? handle))
            {
                using (handle)
                {
                    handle.Set();
                }
            }
        }
        catch (Exception)
        {
            // Best-effort: if signaling fails, the second instance still exits.
        }
    }

    /// <summary>Waits (on a worker thread) for show-window signals from second launches.</summary>
    public void ListenForShowWindowRequests(Action onShowRequested)
    {
        if (_showWindowEvent is null)
        {
            return;
        }

        var listener = _showWindowEvent;
        var thread = new Thread(() =>
        {
            try
            {
                while (listener.WaitOne())
                {
                    onShowRequested();
                }
            }
            catch (Exception)
            {
                // The event was disposed during shutdown; exit quietly.
            }
        })
        {
            IsBackground = true,
            Name = "ShowWindowListener",
        };
        thread.Start();
    }

    public void Dispose()
    {
        _showWindowEvent?.Dispose();
        _showWindowEvent = null;

        if (_owned && _mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Thread did not own the mutex; nothing to release.
            }
        }

        _mutex?.Dispose();
        _mutex = null;
        _owned = false;
    }
}
