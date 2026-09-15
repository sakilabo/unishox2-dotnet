// 文字列の途中にある UUID を専用形式で圧縮し、C 実装とも相互展開できることを確認する。

using System;
using System.Collections.Generic;
using System.Linq;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class NativeUuidCompressionTests
{
    // UUID の大文字・小文字、前後の ASCII・日本語を組み合わせる。
    private static readonly (string Label, string Text)[] Cases =
    {
        ("ascii-lower-trailing", "Hello 550e8400-e29b-41d4-a716-446655440000 World"),
        ("ascii-upper-trailing", "Hello 550E8400-E29B-41D4-A716-446655440000 World"),
        ("japanese-lower-trailing", "こんにちは550e8400-e29b-41d4-a716-446655440000です"),
        ("japanese-upper-trailing", "こんにちは550E8400-E29B-41D4-A716-446655440000です"),
        ("mixed-case-no-trailing", "id=Fa01b51e-7ECC-4e3e-be7b-918A4C2c891C"),
    };

    public static IEnumerable<object[]> CaseData => Cases.Select(c => new object[] { c.Label, c.Text });

    [SkippableTheory]
    [MemberData(nameof(CaseData))]
    public void UpstreamNativeHarness_OldCCompress_NewCSharpDecompress_RoundTrips(string label, string text)
    {
        _ = label;
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(text);
        string hexIn = ToHex(utf8);

        string cCompressedHex = harness.Send($"C 0 {hexIn}");
        Assert.NotEqual("ERR", cCompressedHex);
        byte[] cRaw = FromHex(cCompressedHex)!;

        string decoded = Unishox2.Decompress(cRaw);
        Assert.Equal(text, decoded);
    }

    [SkippableTheory]
    [MemberData(nameof(CaseData))]
    public void UpstreamNativeHarness_NewCSharpCompress_UnmodifiedCDecompress_RoundTrips(string label, string text)
    {
        _ = label;
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(text);

        byte[] csRaw = Unishox2.Compress(text);

        string cDecompressedHex = harness.Send($"D 0 {ToHex(csRaw)}");
        Assert.NotEqual("ERR", cDecompressedHex);
        byte[]? cDecoded = FromHex(cDecompressedHex);
        Assert.NotNull(cDecoded);
        Assert.True(cDecoded!.SequenceEqual(utf8),
            $"無改造のsiara-cc/Unishox2 C バイナリでの展開結果が一致しません。cs圧縮={ToHex(csRaw)}");
    }

    [Theory]
    [MemberData(nameof(CaseData))]
    public void CSharp_UuidNotAtStart_RoundTrips(string label, string text)
    {
        _ = label;
        var compressed = Unishox2.Compress(text);
        string decompressed = Unishox2.Decompress(compressed);
        Assert.Equal(text, decompressed);
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
