// tests/upstream/Unishox2/test_unishox2.c の run_unit_tests() からテキストを抽出した
// 公式テストケース(UpstreamTestCases.json、159 件)で、C# 実装の往復性を検証する。
// siara-cc/Unishox2CとC#の双方向の相互展開を検証する。圧縮バイト列の完全一致は要求しない。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class UpstreamCompatibilityTests
{
    private static readonly string[] Cases = LoadCases();

    private static string[] LoadCases()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "UpstreamTestCases.json");
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
    }

    // siara-cc/Unishox2テストコード中に同一文字列のテストケースが複数箇所で使われているため、文字列だけを
    // MemberData のキーにすると xUnit のテストケース ID が重複して警告になる。検証範囲を
    // 減らさず(159 件すべてを維持し)ID だけを一意にするため、ケース番号を組で渡す。
    public static IEnumerable<object[]> CaseData => Cases.Select((c, i) => new object[] { i, c });

    [Theory]
    [MemberData(nameof(CaseData))]
    public void UpstreamOfficialTestCase_RoundTripsInCSharp(int caseNumber, string text)
    {
        _ = caseNumber; // ID 重複回避のためだけの引数(アサーションには使わない)
        var compressed = Unishox2.Compress(text);
        string decompressed = Unishox2.Decompress(compressed);
        Assert.Equal(text, decompressed);
    }

    [SkippableFact]
    public void UpstreamOfficialTestCases_CrossDecompress_WhenNativeHarnessAvailable()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        int checkedCount = 0;
        var mismatches = new List<string>();

        foreach (string text in Cases)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(text);
            string hexIn = ToHex(utf8);

            // C# で圧縮 -> C で展開
            byte[] csSaved = Unishox2.Compress(text);
            string cDecompressedHex = harness.Send($"D 0 {ToHex(csSaved)}");
            if (cDecompressedHex == "ERR" || FromHex(cDecompressedHex) is not { } cDecoded || !cDecoded.SequenceEqual(utf8))
            {
                mismatches.Add($"[C#->C decompress mismatch] {Describe(text)}");
                continue;
            }

            // C で圧縮 -> C# で展開
            string cCompressedHex = harness.Send($"C 0 {hexIn}");
            if (cCompressedHex == "ERR")
            {
                mismatches.Add($"[C compress failed] {Describe(text)}");
                continue;
            }
            byte[] cSaved = FromHex(cCompressedHex)!;
            string csDecoded = Unishox2.Decompress(cSaved);
            if (csDecoded != text)
            {
                mismatches.Add($"[C->C# decompress mismatch] {Describe(text)}");
                continue;
            }

            checkedCount++;
        }

        Assert.True(checkedCount > 0, "ネイティブハーネスは利用可能と判定されたが、1 件も検証できなかった。");
        Assert.True(mismatches.Count == 0, $"{mismatches.Count}/{Cases.Length} 件不一致:\n" + string.Join("\n", mismatches.Take(20)));
    }

    private static string Describe(string text) => text.Length > 40 ? text.Substring(0, 40) + "..." : text;

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
