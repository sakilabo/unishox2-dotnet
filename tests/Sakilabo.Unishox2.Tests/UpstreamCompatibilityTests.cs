// Verifies the round-trip behaviour of the C# implementation against the official test cases
// (UpstreamTestCases.json, 159 entries) extracted from run_unit_tests() in
// tests/upstream/Unishox2/test_unishox2.c.
// Cross-decompression is checked in both directions between the siara-cc/Unishox2 C implementation and C#.
// An exact match of the compressed bytes is not required.

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

    // The siara-cc/Unishox2 test code uses the same string in several test cases, so keying MemberData on
    // the string alone would produce duplicate xUnit test case IDs and a warning. The case number is passed
    // alongside the string to make the IDs unique without narrowing coverage, keeping all 159 entries.
    public static IEnumerable<object[]> CaseData => Cases.Select((c, i) => new object[] { i, c });

    [Theory]
    [MemberData(nameof(CaseData))]
    public void UpstreamOfficialTestCase_RoundTripsInCSharp(int caseNumber, string text)
    {
        _ = caseNumber; // present only to keep the test IDs unique; not used in assertions
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

            // Compress in C#, decompress in C
            byte[] csSaved = Unishox2.Compress(text);
            string cDecompressedHex = harness.Send($"D 0 {ToHex(csSaved)}");
            if (cDecompressedHex == "ERR" || FromHex(cDecompressedHex) is not { } cDecoded || !cDecoded.SequenceEqual(utf8))
            {
                mismatches.Add($"[C#->C decompress mismatch] {Describe(text)}");
                continue;
            }

            // Compress in C, decompress in C#
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

        Assert.True(checkedCount > 0, "The native harness was reported as available, but not a single case could be verified.");
        Assert.True(mismatches.Count == 0, $"{mismatches.Count}/{Cases.Length} mismatches:\n" + string.Join("\n", mismatches.Take(20)));
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
