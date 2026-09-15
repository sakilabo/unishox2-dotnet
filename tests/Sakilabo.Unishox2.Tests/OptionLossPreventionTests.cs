// 定義済み設定で表現できない文字は読み飛ばさず、UnishoxFormatException を送出する。

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
        // '!' は AlphaNumOnly の文字集合(英数字と一部記号)に含まれない。
        Assert.Throws<UnishoxFormatException>(() => Unishox2.Compress("Hello!!!", options));
    }

    /// <summary>
    /// NoUnicode でも Unicode 文字は直接ビットパターンで符号化され、往復できる。
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
