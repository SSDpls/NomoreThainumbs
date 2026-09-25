using NoMoreThaiNums.Conversion;

namespace NoMoreThaiNums.Keyboard;

/// <summary>What the hook should do with an intercepted key-down event.</summary>
public enum KeyboardAction
{
    /// <summary>Let the event pass through untouched.</summary>
    Passthrough,

    /// <summary>Swallow the event and inject the mapped ASCII digit instead.</summary>
    ReplaceWithDigit,

    /// <summary>Swallow the event entirely (no injection).</summary>
    Suppress,
}

/// <summary>
/// Decides what to do with each low-level keyboard event.
///
/// Detection is layout-driven: the caller asks Windows which character the
/// active keyboard layout produces for the pressed key (including the real
/// Shift state) and passes that character here. Conversion happens only when
/// the produced character is a Thai numeral (U+0E50–U+0E59). This makes the
/// utility work with Thai Kedmanee (digits on the Shift layer), Thai
/// Pattachote (digits unshifted), other Thai layout variants, and multiple
/// installed layouts — while any key that does not produce a Thai numeral
/// (letters, punctuation, shortcuts) passes through untouched.
///
/// This class contains all decision logic so it can be unit-tested without a
/// real hook. The per-event cost is a handful of comparisons.
///
/// PRIVACY: this class never records, stores, or transmits keystrokes. It only
/// answers one question per event: "does this key-down produce a Thai numeral
/// that should become a Western digit?".
/// </summary>
public sealed class KeyboardProcessor
{
    /// <summary>
    /// When true, Thai numerals are converted. When false every event passes
    /// through and the system keyboard behaves completely normally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Decides the action for one low-level event. Called on the hook thread.
    /// </summary>
    /// <param name="vkCode">Virtual-key code from KBDLLHOOKSTRUCT.</param>
    /// <param name="isKeyDown">True for WM_KEYDOWN/WM_SYSKEYDOWN, false for key-up.</param>
    /// <param name="isInjected">True when the event was injected (LLKHF_INJECTED).</param>
    /// <param name="ctrlDown">True when either Ctrl key is currently down.</param>
    /// <param name="altDown">True when either Alt key is currently down.</param>
    /// <param name="winDown">True when either Windows key is currently down.</param>
    /// <param name="producedChar">
    /// The character the active layout produces for this key, or a non-character
    /// (e.g. '\0') when the caller could not or need not resolve one.
    /// </param>
    /// <param name="digit">The ASCII digit to inject, valid only for ReplaceWithDigit.</param>
    public KeyboardAction Process(
        uint vkCode,
        bool isKeyDown,
        bool isInjected,
        bool ctrlDown,
        bool altDown,
        bool winDown,
        char producedChar,
        out char digit)
    {
        digit = default;

        // Never touch our own injected input: prevents recursion.
        if (isInjected)
        {
            return KeyboardAction.Passthrough;
        }

        // Conversion applies only to plain typing key-downs. Every modifier
        // combination that forms shortcuts (Ctrl+X, Alt+X, Win+X) must behave
        // exactly like Windows without this utility running. Shift is allowed:
        // Kedmanee needs it to reach the Thai numerals.
        if (!Enabled || !isKeyDown || ctrlDown || altDown || winDown)
        {
            return KeyboardAction.Passthrough;
        }

        char? converted = ThaiDigitConverter.TryConvert(producedChar);
        if (!converted.HasValue)
        {
            return KeyboardAction.Passthrough;
        }

        digit = converted.Value;
        return KeyboardAction.ReplaceWithDigit;
    }
}
