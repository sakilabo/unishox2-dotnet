// lines の要素間参照と、現在の出力バッファに重なる自己参照を確認する。

using System.Collections.Generic;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class LinesAndSelfReferenceTests
{
    public static IEnumerable<object[]> ElementSets => new List<object[]>
    {
        new object[] { new[] { "line one", "line two", "line three" } },
        new object[] { new[] { "same text", "same text", "same text" } },
        new object[] { new[] { "", "second", "", "", "fifth" } },
        new object[] { new[] { "" } },
        new object[] { new[] { "", "" } },
    };

    [Theory]
    [MemberData(nameof(ElementSets))]
    public void CompressLines_DecompressesToOriginalElements(string[] elements)
    {
        var compressed = Unishox2.CompressLines(elements);
        Assert.Equal(elements.Length, compressed.Length);

        string[] decompressed = Unishox2.DecompressLines(compressed);
        Assert.Equal(elements, decompressed);
    }

    // 未出力範囲まで伸びる自己参照を、逐次コピーで復元する。
    [Theory]
    [InlineData("XAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXA")]
    [InlineData("ABABABABABABABABABABABABABABABABABABAB")]
    [InlineData("ABCABCABCABCABCABCABCABCABCABCABCABCABC")]
    [InlineData("RepeatRepeatRepeatRepeatRepeatEnd")]
    [InlineData("012301230123012301230123012301230123")]
    public void SelfReferencingOverlapWithinSingleElement_RoundTrips(string text)
    {
        string[] elements = { text, "tail" };

        var compressed = Unishox2.CompressLines(elements);
        string[] decompressed = Unishox2.DecompressLines(compressed);

        Assert.Equal(elements, decompressed);
    }

    [Fact]
    public void RoundTrip_WithExplicitOption_PreservesElements()
    {
        var options = CompressOptions.Json;
        string[] elements = { "{\"a\":1}", "{\"b\":2}", "", "{\"c\":3}" };

        var compressed = Unishox2.CompressLines(elements, options);
        string[] decompressed = Unishox2.DecompressLines(compressed, options);

        Assert.Equal(elements, decompressed);
        Assert.Equal(elements.Length, compressed.Length);
    }

    [Fact]
    public void Utf8ArrayInput_DecompressesWithDefaultOptions()
    {
        string[] expected = { "", "日本語\0😀日本語\0😀", "日本語\0😀", "" };
        byte[][] input = expected.Select(System.Text.Encoding.UTF8.GetBytes).ToArray();
        byte[][] data = Unishox2.CompressLines(input);
        Assert.Equal(expected, Unishox2.DecompressLines(data));
    }

    [Fact]
    public void Utf8ArrayInput_RejectsInvalidUtf8()
    {
        Assert.Throws<UnishoxFormatException>(() =>
            Unishox2.CompressLines(new[] { new byte[] { 0xC0, 0x80 } }));
    }
}
