// lines 機能で NUL を通常のデータとして扱う仕様の検証。
// NativeLinesNulByteTests.cs はsiara-cc/Unishox2(C)が生成した圧縮データを Sakilabo.Unishox2 が正しく
// 展開できることを検証する。このファイルは Sakilabo.Unishox2 自身が NUL 以降を参照検索の
// 対象に含めること自体を、内部の TryMatchLine を直接呼んで検証する
// (単なる往復検証だけでは、参照検索が実際に NUL を跨いだ/越えたかを確認できないため)。

using System.Linq;
using System.Text;
using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2.Tests;

public class LinesNulByteReferenceTests
{
    /// <summary>
    /// 現在要素の自己参照(ctx=0)。"AB\0CDEFG" を 2 回繰り返した 16 バイトのうち、
    /// 2 回目(l=8..15)は 1 回目(0..7)全体と一致する。この一致区間は index 2 の NUL を
    /// 跨ぐため、参照検索が NUL を跨いで一致を見つけることを直接確認する。
    /// </summary>
    [Fact]
    public void TryMatchLine_FindsSelfReferenceSpanningEmbeddedNul()
    {
        byte[] unit = Encoding.UTF8.GetBytes("AB\0CDEFG");
        byte[] data = unit.Concat(unit).ToArray();
        var chain = new CompressLineChain(data, System.Array.Empty<byte[]>());
        var writer = new BitWriter();
        HCodeGroup state = HCodeGroup.Alpha;

        bool found = LineMatching.TryMatchLine(
            data, unit.Length, writer, chain, ref state,
            Tables.HCodesDefault, out int newL);

        Assert.True(found);
        Assert.Equal(unit.Length + unit.Length - 1, newL);
    }

    /// <summary>
    /// 直前の要素(ctx=1)が "X\0HELLO!" で、NUL(index 1)の後ろにしか一致対象("HELLO!")が
    /// 無い場合。strlen() で打ち切ると previous の可視範囲は "X" の 1 バイトだけになり、
    /// この一致は原理的に見つけられない。GetLength が全長を返すことで、NUL より後ろの
    /// 領域も参照検索の対象になることを直接確認する。
    /// </summary>
    [Fact]
    public void TryMatchLine_FindsPreviousElementReferenceLocatedAfterEmbeddedNul()
    {
        byte[] previous = Encoding.UTF8.GetBytes("X\0HELLO!");
        byte[] current = Encoding.UTF8.GetBytes("HELLO!");
        var chain = new CompressLineChain(current, new[] { previous });
        var writer = new BitWriter();
        HCodeGroup state = HCodeGroup.Alpha;

        bool found = LineMatching.TryMatchLine(
            current, 0, writer, chain, ref state,
            Tables.HCodesDefault, out int newL);

        Assert.True(found);
        Assert.Equal(current.Length - 1, newL);
    }

    /// <summary>
    /// 自己参照の重なりコピー(短周期反復)が、NUL を跨いだ状態でも正しく往復することを
    /// 公開 API で確認する(DecodeRepeat の 1 バイトずつのコピーが NUL 込みでも成立するか)。
    /// </summary>
    [Theory]
    [InlineData("AB\0AB\0AB\0AB\0AB\0AB\0AB\0AB\0")]
    [InlineData("A\0A\0A\0A\0A\0A\0A\0A\0A\0A\0")]
    public void SelfOverlappingReference_AcrossEmbeddedNul_RoundTrips(string unit)
    {
        string[] elements = { unit, "tail" };

        var compressed = Unishox2.CompressLines(elements);
        Assert.Equal(elements, Unishox2.DecompressLines(compressed));
    }

    /// <summary>
    /// 直前の要素の NUL より後ろの内容("HELLOWORLD")を、次の要素が参照する場合の往復。
    /// </summary>
    [Fact]
    public void CrossElementReference_ToContentAfterEmbeddedNul_RoundTrips()
    {
        string[] elements = { "X\0HELLOWORLD", "HELLOWORLD" };

        var compressed = Unishox2.CompressLines(elements);
        Assert.Equal(elements, Unishox2.DecompressLines(compressed));
    }

    /// <summary>
    /// マルチバイト UTF-8 文字と NUL が混在する要素の自己参照往復。UTF-8 の部分一致回避
    /// (TryMatchLine の継続バイト巻き戻し)が、NUL を跨ぐ拡張後の範囲でも文字境界を壊さない
    /// ことを確認する。
    /// </summary>
    [Fact]
    public void EmbeddedNulWithMultibyteUtf8_SelfReference_RoundTrips()
    {
        string unit = "日本語\0";
        string element = string.Concat(System.Linq.Enumerable.Repeat(unit, 5)) + "tail日本語";
        string[] elements = { element, "tail" };

        var compressed = Unishox2.CompressLines(elements);
        Assert.Equal(elements, Unishox2.DecompressLines(compressed));
    }
}
