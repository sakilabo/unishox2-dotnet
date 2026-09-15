// Characters that a predefined setting cannot represent are not silently skipped; UnishoxFormatException is thrown.

using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class OptionLossPreventionTests
{
    [Fact]
    public void AlphaOnlyOption_WithDigits_ThrowsInsteadOfSilentlyDroppingData()
    {
        var options = CompressOptions.AlphaOnly;
        var ex = Assert.Throws<UnishoxFormatException>(() => Unishox2.Compress("Hello123", options));
        Assert.Contains("CompressOptions", ex.Message);
    }

    [Fact]
    public void AlphaOnlyOption_WithLettersAndSpacesOnly_Succeeds()
    {
        var options = CompressOptions.AlphaOnly;
        var compressed = Unishox2.Compress("Hello World", options);
        Assert.Equal("Hello World", Unishox2.Decompress(compressed, options));
    }

    [Fact]
    public void AlphaNumOnlyOption_WithSymbols_ThrowsInsteadOfSilentlyDroppingData()
    {
        var options = CompressOptions.AlphaNumOnly;
        // '!' is not in the AlphaNumOnly character set of letters, digits and a few symbols.
        Assert.Throws<UnishoxFormatException>(() => Unishox2.Compress("Hello!!!", options));
    }

    /// <summary>
    /// Even under NoUnicode, Unicode characters are encoded directly as bit patterns and round-trip correctly.
    /// </summary>
    [Fact]
    public void NoUnicodeOption_WithUnicodeInput_DoesNotLoseData()
    {
        var options = CompressOptions.NoUnicode;
        var compressed = Unishox2.Compress("こんにちは", options);
        Assert.Equal("こんにちは", Unishox2.Decompress(compressed, options));
    }

    [Fact]
    public void LinesElements_OneElementLossy_ThrowsAndDoesNotReturnPartialResult()
    {
        var options = CompressOptions.AlphaOnly;
        Assert.Throws<UnishoxFormatException>(() => Unishox2.CompressLines(new[] { "ok", "bad123", "ok2" }, options));
    }
}
