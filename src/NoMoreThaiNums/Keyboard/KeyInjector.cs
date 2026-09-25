using System.Runtime.InteropServices;
using NoMoreThaiNums.Logging;

namespace NoMoreThaiNums.Keyboard;

/// <summary>
/// Injects replacement ASCII digits as KEYEVENTF_UNICODE events.
///
/// Unicode injection was chosen over scancode injection because it produces
/// the exact target character regardless of which keyboard layout is active
/// at that instant, and because it carries no modifier side effects.
///
/// The injected events are tagged with <see cref="Marker"/> in dwExtraInfo so
/// our hook can recognize and ignore them (no recursion). Injection happens
/// on the hook thread by design: it occurs immediately after a swallow, before
/// the hook returns, which keeps digit ordering correct under auto-repeat and
/// fast typing. Sending one INPUT takes microseconds; this does not threaten
/// the low-level hook timeout.
/// </summary>
internal static class KeyInjector
{
    /// <summary>Extra-info marker stamped onto every event we inject.</summary>
    public static readonly IntPtr Marker = new(0x544E4639); // "TNF9"

    /// <summary>
    /// Sends a single Unicode character as a down+up pair.
    /// Returns true when both events were accepted by the OS.
    /// </summary>
    public static bool SendUnicodeChar(char c)
    {
        var inputs = new NativeInterop.INPUT[2];

        inputs[0].type = NativeInterop.INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = 0;
        inputs[0].u.ki.wScan = c;
        inputs[0].u.ki.dwFlags = NativeInterop.KEYEVENTF_UNICODE;
        inputs[0].u.ki.time = 0;
        inputs[0].u.ki.dwExtraInfo = Marker;

        inputs[1].type = NativeInterop.INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = 0;
        inputs[1].u.ki.wScan = c;
        inputs[1].u.ki.dwFlags = NativeInterop.KEYEVENTF_UNICODE | NativeInterop.KEYEVENTF_KEYUP;
        inputs[1].u.ki.time = 0;
        inputs[1].u.ki.dwExtraInfo = Marker;

        try
        {
            uint sent = NativeInterop.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInterop.INPUT>());
            if (sent != inputs.Length)
            {
                ErrorLog.Write($"SendInput sent {sent} of {inputs.Length} events for U+{(int)c:X4}.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Write("SendInput threw an exception.", ex);
            return false;
        }
    }
}
