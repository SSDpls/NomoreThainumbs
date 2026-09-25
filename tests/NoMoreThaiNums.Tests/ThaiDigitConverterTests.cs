using NoMoreThaiNums.Conversion;
using Xunit;

namespace NoMoreThaiNums.Tests;

public class ThaiDigitConverterTests
{
    [Theory]
    [InlineData('๐', '0')]
    [InlineData('๑', '1')]
    [InlineData('๒', '2')]
    [InlineData('๓', '3')]
    [InlineData('๔', '4')]
    [InlineData('๕', '5')]
    [InlineData('๖', '6')]
    [InlineData('๗', '7')]
    [InlineData('๘', '8')]
    [InlineData('๙', '9')]
    public void TryConvert_MapsEveryThaiDigit(char thai, char expected)
    {
        char? result = ThaiDigitConverter.TryConvert(thai);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData('0')]
    [InlineData('5')]
    [InlineData('9')]
    [InlineData('ก')]
    [InlineData('ๆ')]
    [InlineData('่')]
    [InlineData(' ')]
    [InlineData('A')]
    public void TryConvert_NonThaiDigit_ReturnsNull(char input)
    {
        Assert.Null(ThaiDigitConverter.TryConvert(input));
    }

    [Fact]
    public void Convert_DigitSequence()
    {
        Assert.Equal("0123456789", ThaiDigitConverter.Convert("๐๑๒๓๔๕๖๗๘๙"));
    }

    [Fact]
    public void Convert_ThaiTextWithDigits_KeepsThaiLetters()
    {
        Assert.Equal("ทดสอบ123", ThaiDigitConverter.Convert("ทดสอบ๑๒๓"));
    }

    [Fact]
    public void Convert_ThaiTextWithoutDigits_IsUnchanged()
    {
        string text = "ทดสอบภาษาไทย";
        Assert.Equal(text, ThaiDigitConverter.Convert(text));
    }

    [Fact]
    public void Convert_MixedSentence()
    {
        Assert.Equal("จำนวน 123 ชิ้น", ThaiDigitConverter.Convert("จำนวน ๑๒๓ ชิ้น"));
    }

    [Fact]
    public void Convert_AsciiText_IsUnchanged()
    {
        Assert.Equal("abc123", ThaiDigitConverter.Convert("abc123"));
    }

    [Fact]
    public void Convert_EmptyAndNull_AreSafe()
    {
        Assert.Equal(string.Empty, ThaiDigitConverter.Convert(string.Empty));
        Assert.Null(ThaiDigitConverter.Convert(null!));
    }

    [Fact]
    public void Convert_ResultsAreRealAsciiCodePoints()
    {
        string converted = ThaiDigitConverter.Convert("๐๑๒๓๔๕๖๗๘๙");
        for (int i = 0; i < converted.Length; i++)
        {
            Assert.Equal(0x30 + i, converted[i]);
        }
    }

    [Fact]
    public void Convert_ToneMarksAndVowelsSurroundingDigits_AreUnchanged()
    {
        // Thai digits convert; neighboring Thai letters, vowels, and tone marks
        // (including combining marks) stay byte-for-byte intact.
        Assert.Equal("ก่อน2", ThaiDigitConverter.Convert("ก่อน๒"));
        Assert.Equal("2่", ThaiDigitConverter.Convert("๒่"));
        Assert.Equal("404Not Found", ThaiDigitConverter.Convert("๔๐๔Not Found"));
    }

    [Fact]
    public void ContainsThaiDigit_DetectsAndRejects()
    {
        Assert.True(ThaiDigitConverter.ContainsThaiDigit("๑๒๓"));
        Assert.True(ThaiDigitConverter.ContainsThaiDigit("abc๑"));
        Assert.False(ThaiDigitConverter.ContainsThaiDigit("ทดสอบภาษาไทย"));
        Assert.False(ThaiDigitConverter.ContainsThaiDigit("abc123"));
        Assert.False(ThaiDigitConverter.ContainsThaiDigit(string.Empty));
        Assert.False(ThaiDigitConverter.ContainsThaiDigit(null!));
    }
}
