// 自己参照の重なりを含むデータについて、C#での往復とsiara-cc/Unishox2圧縮データの展開を検証する。

using System;
using System.Linq;
using System.Text;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class NativeLinesBugTests
{
    [SkippableFact]
    public void UpstreamCompressedSelfReferenceOverlap_DecompressesInCSharp()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);
        const string element = "XAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXA";
        string compressedHex = harness.Send($"L 0 {ToHex(Encoding.UTF8.GetBytes(element))}");
        Assert.NotEqual("ERR", compressedHex);
        byte[][] compressed = { FromHex(compressedHex)! };
        Assert.Equal(new[] { element }, Unishox2.DecompressLines(compressed));
    }
    [Fact]
    public void CSharp_SelfReferenceOverlap_RoundTripsCorrectly()
    {
        // siara-cc/Unishox2と同じ入力を、C# 側(実運用パターン: DecompressLineChain の ctx=0 は
        // 現在の展開済み出力バッファのみを参照し、原文には一切触れない)で検証する。
        string element = "XAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXA";
        string[] elements = { element, "tail" };

        var compressed = Unishox2.CompressLines(elements);
        string[] decompressed = Unishox2.DecompressLines(compressed);

        Assert.Equal(elements, decompressed);
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
