using NoMoreThaiNums.Keyboard;
using Xunit;

namespace NoMoreThaiNums.Tests;

public class KeyboardProcessorTests
{
    private static KeyboardAction Process(
        KeyboardProcessor processor,
        char producedChar,
        out char digit,
        bool isKeyDown = true,
        bool isInjected = false,
        bool ctrl = false,
        bool alt = false,
        bool win = false)
    {
        return processor.Process(
            vkCode: 0x00, // vk is no longer the conversion criterion; kept for completeness
            isKeyDown: isKeyDown,
            isInjected: isInjected,
            ctrlDown: ctrl,
            altDown: alt,
            winDown: win,
            producedChar: producedChar,
            out digit);
    }

    [Theory]
    [InlineData('๑', '1')]
    [InlineData('๒', '2')]
    [InlineData('๓', '3')]
    [InlineData('๔', '4')]
    [InlineData('๕', '5')]
    [InlineData('๖', '6')]
    [InlineData('๗', '7')]
    [InlineData('๘', '8')]
    [InlineData('๙', '9')]
    [InlineData('๐', '0')]
    public void KeyDownProducingThaiNumeral_IsReplaced(char thai, char expectedDigit)
    {
        var processor = new KeyboardProcessor { Enabled = true };
        KeyboardAction action = Process(processor, thai, out char digit);

        Assert.Equal(KeyboardAction.ReplaceWithDigit, action);
        Assert.Equal(expectedDigit, digit);
    }

    [Fact]
    public void ShiftedKeyProducingThaiNumeral_IsReplaced()
    {
        // Kedmanee produces Thai numerals on the Shift layer, so Shift must
        // NOT suppress conversion (unlike Ctrl/Alt/Win).
        var processor = new KeyboardProcessor { Enabled = true };
        KeyboardAction action = processor.Process(
            vkCode: 0x32,
            isKeyDown: true,
            isInjected: false,
            ctrlDown: false,
            altDown: false,
            winDown: false,
            producedChar: '๒',
            out char digit);

        Assert.Equal(KeyboardAction.ReplaceWithDigit, action);
        Assert.Equal('2', digit);
    }

    [Fact]
    public void WhenDisabled_EverythingPassesThrough()
    {
        var processor = new KeyboardProcessor { Enabled = false };
        KeyboardAction action = Process(processor, '๑', out char digit);

        Assert.Equal(KeyboardAction.Passthrough, action);
        Assert.Equal(default, digit);
    }

    [Fact]
    public void KeyUp_AlwaysPassesThrough()
    {
        var processor = new KeyboardProcessor { Enabled = true };
        KeyboardAction action = Process(processor, '๑', out _, isKeyDown: false);

        Assert.Equal(KeyboardAction.Passthrough, action);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void ShortcutModifiers_PassThrough(bool ctrl, bool alt, bool win)
    {
        var processor = new KeyboardProcessor { Enabled = true };
        KeyboardAction action = Process(processor, '๑', out _, ctrl: ctrl, alt: alt, win: win);

        Assert.Equal(KeyboardAction.Passthrough, action);
    }

    [Fact]
    public void InjectedEvent_AlwaysPassesThrough_PreventingRecursion()
    {
        var processor = new KeyboardProcessor { Enabled = true };
        KeyboardAction action = Process(processor, '๑', out _, isInjected: true);

        Assert.Equal(KeyboardAction.Passthrough, action);
    }

    [Theory]
    [InlineData('ก')]  // Thai letter: must never be touched
    [InlineData('/')]  // Kedmanee unshifted "2" key produces '/'
    [InlineData('ู')]  // combining vowel from unshifted keys
    [InlineData('a')]
    [InlineData('5')]  // already a Western digit: nothing to do
    [InlineData('\0')] // no character resolved (dead key, F-key, etc.)
    public void NonThaiNumeralCharacters_PassThrough(char produced)
    {
        var processor = new KeyboardProcessor { Enabled = true };
        KeyboardAction action = Process(processor, produced, out char digit);

        Assert.Equal(KeyboardAction.Passthrough, action);
        Assert.Equal(default, digit);
    }
}
