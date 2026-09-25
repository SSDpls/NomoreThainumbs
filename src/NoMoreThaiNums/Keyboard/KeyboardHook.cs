using System.Runtime.InteropServices;
using System.Text;
using NoMoreThaiNums.Logging;

namespace NoMoreThaiNums.Keyboard;

/// <summary>
/// Installs the global WH_KEYBOARD_LL low-level keyboard hook on a dedicated
/// message-pump thread and forwards raw events to the processor.
///
/// Design notes:
/// - The hook is installed from a dedicated background thread that runs its own
///   message pump, so the WinForms UI thread can never stall typing.
/// - The callback does minimal work: read the struct, resolve which character
///   the ACTIVE layout produces for the key (only for plain key-downs), ask the
///   processor for a decision, then pass through or swallow+inject.
/// - Character resolution uses the foreground window's thread keyboard layout
///   via ToUnicodeEx with a synthesized Shift state. This follows language
///   switches, multiple installed layouts, and Shift automatically. Only the
///   first resolved character is examined; the mapping
///   (U+0E50-U+0E59 to U+0030-U+0039) is a constant-time arithmetic lookup.
/// - Injected events (LLKHF_INJECTED) pass through unmodified: no recursion.
/// - Cleanup: WM_QUIT stops the pump; Dispose unhooks and joins the thread.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int KeyStateSize = 256;

    // Reused buffers for ToUnicodeEx; the hook thread is the only user.
    private readonly byte[] _keyState = new byte[KeyStateSize];
    private readonly StringBuilder _charBuffer = new(8);

    private readonly KeyboardProcessor _processor;
    private readonly NativeInterop.LowLevelKeyboardProc _proc;
    private readonly ManualResetEventSlim _readyEvent = new(false);

    private Thread? _hookThread;
    private IntPtr _hookHandle = IntPtr.Zero;
    private uint _hookThreadId;
    private bool _disposed;

    public KeyboardHook(KeyboardProcessor processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _proc = HookCallback;
    }

    /// <summary>Starts the hook thread and installs the hook. Throws on failure.</summary>
    public void Start()
    {
        if (_hookThread is not null)
        {
            throw new InvalidOperationException("KeyboardHook was already started.");
        }

        _hookThread = new Thread(HookThreadMain)
        {
            IsBackground = true,
            Name = "NoMoreThaiNums.KeyboardHook",
        };
        _hookThread.Start();

        if (!_readyEvent.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException(
                "The keyboard hook thread did not initialize within 5 seconds.");
        }

        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "SetWindowsHookEx(WH_KEYBOARD_LL) failed. A message loop is required on the installing thread.");
        }
    }

    private void HookThreadMain()
    {
        _hookThreadId = GetCurrentThreadId();

        IntPtr module = NativeInterop.GetModuleHandle(null);
        _hookHandle = NativeInterop.SetWindowsHookEx(
            NativeInterop.WH_KEYBOARD_LL, _proc, module, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            ErrorLog.Write($"SetWindowsHookEx failed with Win32 error {error}.");
            _readyEvent.Set();
            return;
        }

        _readyEvent.Set();

        // Message loop: the LL hook requires this thread to pump messages.
        while (NativeInterop.GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
            // No thread messages are expected; dispatch is a no-op for us.
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                var info = Marshal.PtrToStructure<NativeInterop.KBDLLHOOKSTRUCT>(lParam);

                bool isKeyDown =
                    wParam == (IntPtr)NativeInterop.WM_KEYDOWN ||
                    wParam == (IntPtr)NativeInterop.WM_SYSKEYDOWN;

                char produced = '\0';
                if (isKeyDown && _processor.Enabled)
                {
                    produced = ResolveProducedCharacter(info.vkCode, info.scanCode);
                }

                bool ctrlDown = IsKeyDown(KeyCodes.VkControl);
                bool altDown = IsKeyDown(KeyCodes.VkMenu);
                bool winDown = IsKeyDown(KeyCodes.VkLWin) || IsKeyDown(KeyCodes.VkRWin);

                char digit;
                KeyboardAction action = _processor.Process(
                    info.vkCode,
                    isKeyDown,
                    (info.flags & NativeInterop.KBDLLHOOKSTRUCT.LLKHF_INJECTED_FLAG) != 0,
                    ctrlDown,
                    altDown,
                    winDown,
                    produced,
                    out digit);

                if (action == KeyboardAction.ReplaceWithDigit)
                {
                    // Swallow the Thai numeral and inject the ASCII digit. If
                    // injection fails, do NOT swallow: the user must still see
                    // the original Thai numeral rather than losing a keystroke.
                    if (KeyInjector.SendUnicodeChar(digit))
                    {
                        return (IntPtr)1;
                    }
                }

                if (action == KeyboardAction.Suppress)
                {
                    return (IntPtr)1;
                }
            }
            catch (Exception ex)
            {
                // Never let an exception escape the hook: that would risk
                // crashing the process that received the callback.
                ErrorLog.Write("Unexpected error inside the keyboard hook callback.", ex);
            }
        }

        return NativeInterop.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    /// Asks Windows which character the ACTIVE layout produces for this key
    /// with the CURRENT physical Shift state. Returns '\0' when the key
    /// produces no character or resolution fails (the processor then passes
    /// the event through untouched).
    /// </summary>
    private char ResolveProducedCharacter(uint vk, uint scanCode)
    {
        // The layout of the thread that owns the foreground window is the one
        // the user is actually typing into.
        IntPtr foregroundWindow = NativeInterop.GetForegroundWindow();
        uint targetThreadId = NativeInterop.GetWindowThreadProcessId(foregroundWindow, out _);
        IntPtr hkl = targetThreadId != 0
            ? NativeInterop.GetKeyboardLayout(targetThreadId)
            : NativeInterop.GetKeyboardLayout(0);

        if (hkl == IntPtr.Zero)
        {
            return '\0';
        }

        Array.Clear(_keyState);
        if (IsKeyDown(KeyCodes.VkShift))
        {
            _keyState[0x10] = 0x80; // VK_SHIFT
            _keyState[0xA0] = 0x80; // VK_LSHIFT
        }

        _charBuffer.Clear();
        int result = NativeInterop.ToUnicodeEx(
            vk, scanCode, _keyState, _charBuffer, _charBuffer.Capacity, 0, hkl);

        // result == 1: one character; 0 or negative: dead key or no character.
        return result == 1 ? _charBuffer[0] : '\0';
    }

    private static bool IsKeyDown(uint vk) => (NativeInterop.GetAsyncKeyState((int)vk) & 0x8000) != 0;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hookThreadId != 0)
        {
            NativeInterop.PostThreadMessage(_hookThreadId, NativeInterop.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        if (_hookThread is not null && _hookThread.IsAlive)
        {
            _hookThread.Join(TimeSpan.FromSeconds(2));
        }

        if (_hookHandle != IntPtr.Zero)
        {
            NativeInterop.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _readyEvent.Dispose();
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
