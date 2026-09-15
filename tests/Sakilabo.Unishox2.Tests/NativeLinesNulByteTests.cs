// NUL を含むsiara-cc/Unishox2 C の圧縮データを C# で復元する。
// NUL 以降への参照は非互換のため、逆方向の展開は要求しない。

using System;
using System.Linq;
using System.Text;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class NativeLinesNulByteTests
{
    private static string RepeatWithEmbeddedNul(string unit, int count) => string.Concat(Enumerable.Repeat(unit, count));

    [SkippableFact]
    public void SelfReferenceAcrossEmbeddedNul_UpstreamDataDecompresses_WhenAvailable()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        // 単一要素の自己参照(ctx=0)。"AB\0" の周期的な繰り返しにより、NUL より後ろを
        // 参照検索に含めるかどうかで一致の有無が変わる。
        string element = RepeatWithEmbeddedNul("AB\0", 10);
        byte[] utf8 = Encoding.UTF8.GetBytes(element);
        string hexIn = ToHex(utf8);

        string compressedHex = harness.Send($"L 0 {hexIn}");
        Assert.NotEqual("ERR", compressedHex);
        byte[] cRaw = FromHex(compressedHex)!;

        // C -> C# 展開(実運用パターン: 現在要素の原文を保持しない)。
        // lines(ctx=0 自己参照込み)でエンコードされたビット列なので、復元時も
        // DecompressLines(DecompressLineChain 経由)を使う必要がある
        // (Decompress だと lines 機能なしの展開ロジックになり、ビット列の解釈が変わる)。
        byte[][] reconstructed = { cRaw };
        Assert.Equal(new[] { element }, Unishox2.DecompressLines(reconstructed));
    }

    [SkippableFact]
    public void CrossElementReferenceAcrossEmbeddedNul_UpstreamDataDecompresses_WhenAvailable()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        // 直前の要素(ctx=1)が NUL を含む。参照検索はsiara-cc/Unishox2の strlen() 相当で
        // NUL より前("AB" の 2 バイト)までに制限され、それより後ろの内容
        // ("AB\0" の繰り返し)は次の要素からの参照検索の対象にならない。
        string previous = RepeatWithEmbeddedNul("AB\0", 10);
        string current = RepeatWithEmbeddedNul("AB\0", 10);
        byte[] prevUtf8 = Encoding.UTF8.GetBytes(previous);
        byte[] curUtf8 = Encoding.UTF8.GetBytes(current);

        string compressedHex = harness.Send($"L 0 {ToHex(prevUtf8)} {ToHex(curUtf8)}");
        Assert.NotEqual("ERR", compressedHex);
        string[] cParts = compressedHex.Split(' ');
        Assert.Equal(2, cParts.Length);
        byte[] cRaw0 = FromHex(cParts[0])!;
        byte[] cRaw1 = FromHex(cParts[1])!;

        string[] elements = { previous, current };

        // C -> C# 展開(DecompressLines は常に lines 扱いなので、上のテストと同じ理由で成立する)
        byte[][] reconstructed = { cRaw0, cRaw1 };
        Assert.Equal(elements, Unishox2.DecompressLines(reconstructed));
    }

    /// <summary>
    /// ネイティブハーネスが無い環境でも、NUL を含む原文が(参照検索の制限とは無関係に)
    /// 完全に保持されることだけは常に検証する。
    /// </summary>
    [Fact]
    public void EmbeddedNulContent_RoundTripsCSharpOnly_RegardlessOfNativeHarnessAvailability()
    {
        string element = RepeatWithEmbeddedNul("AB\0", 10);
        string[] elements = { element, element, "tail" };

        var compressed = Unishox2.CompressLines(elements);
        Assert.Equal(elements, Unishox2.DecompressLines(compressed));
    }

    private static string ToHex(byte[] data)
    {
        var chars = new char[data.Length * 2];
        const string hexDigits = "0123456789abcdef";
        for (int i = 0; i < data.Length; i++)
        {
            chars[i * 2] = hexDigits[data[i] >> 4];
            chars[i * 2 + 1] = hexDigits[data[i] & 0xF];
        }
        return new string(chars);
    }

    private static byte[]? FromHex(string hex)
    {
        if (hex.Length % 2 != 0) return null;
        var result = new byte[hex.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return result;
    }
}
