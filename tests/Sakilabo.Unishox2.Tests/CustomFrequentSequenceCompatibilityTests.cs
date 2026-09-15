// 独自の FrequentSequences / Templates が UTF-8 バイト列(ASCII に限らない)として
// 正しく扱われることを検証する。siara-cc/Unishox2 unishox2.c は頻出文字列を const char* の
// バイト列比較・コピーとして扱っており、ASCII に限定していないため、
// 日本語を含む頻出文字列でもsiara-cc/Unishox2(ネイティブハーネスの X/Y コマンド)と
// 相互展開できることを確認する。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class CustomFrequentSequenceCompatibilityTests
{
    private static readonly string[] JapaneseFreqSeq =
        { "こんにちは", "ありがとう", "さようなら", "おはよう", "こんばんは", "すみません" };

    [Fact]
    public void CustomJapaneseFrequentSequences_RoundTripInCSharp()
    {
        var options = new CompressOption { FrequentSequences = JapaneseFreqSeq };
        string text = "こんにちはさようなら123";
        var compressed = Unishox2.Compress(text, options);
        Assert.Equal(text, Unishox2.Decompress(compressed, options));
    }

    [SkippableFact]
    public void CustomJapaneseFrequentSequences_MatchesNativeHarness_WhenAvailable()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        var options = new CompressOption { FrequentSequences = JapaneseFreqSeq };
        string[] texts =
        {
            "こんにちはさようなら123",
            "こんにちはこんにちはこんにちは",
            "ありがとうございました。またおはよう。",
            "Hello こんばんは World すみません",
        };

        string freqArgs = string.Join(" ", JapaneseFreqSeq.Select(f => ToHex(Encoding.UTF8.GetBytes(f))));

        var mismatches = new List<string>();
        foreach (string text in texts)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(text);
            byte[] csRaw = Unishox2.Compress(text, options);

            string cCompressedHex = harness.Send($"X 0 {freqArgs} {ToHex(utf8)}");
            if (cCompressedHex == "ERR")
            {
                mismatches.Add($"C compress failed: {text}");
                continue;
            }
            byte[] cRaw = FromHex(cCompressedHex)!;
            Assert.Equal(text, Unishox2.Decompress(cRaw, options));

            string cDecompressedHex = harness.Send($"Y 0 {freqArgs} {ToHex(csRaw)}");
            if (cDecompressedHex == "ERR" || FromHex(cDecompressedHex) is not { } cDecoded || !cDecoded.SequenceEqual(utf8))
            {
                mismatches.Add($"C decompress mismatch: {text}");
            }
        }

        Assert.True(mismatches.Count == 0, string.Join("\n", mismatches));
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
