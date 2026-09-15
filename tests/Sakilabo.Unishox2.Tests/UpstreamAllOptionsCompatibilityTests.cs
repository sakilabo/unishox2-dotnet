// Cross-decompresses all 17 predefined settings, each with input it can represent.

using System;
using System.Collections.Generic;
using System.Linq;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class UpstreamAllOptionsCompatibilityTests
{
    // Static fields in C# are initialized in source declaration order, so every *Cases array that
    // OptionCases refers to must be declared before OptionCases.
    private static readonly string[] AlphaOnlyCases =
    {
        "Hello",
        "Hello World",
        "HELLO WORLD",
        "The quick brown fox jumped over the lazy dog",
    };

    private static readonly string[] AlphaNumCases = AlphaOnlyCases.Concat(new[]
    {
        "Hello123 World456",
        "1234567890",
    }).ToArray();

    private static readonly string[] AlphaNumSymCases = AlphaNumCases.Concat(new[]
    {
        "Hello, World! (123)",
        "a.b-c_d/e=f+g$h%i#j",
    }).ToArray();

    private static readonly string[] AsciiAndUnicodeCases = AlphaNumSymCases.Concat(new[]
    {
        "{\"key\": \"value\"}",
        "https://example.com/path",
        "こんにちは世界",
        "日本語とEnglishの混在123",
    }).ToArray();

    // The setting numbers of the C implementation run from 0 to 16, matching get_preset() in
    // tests/upstream/harness/harness.c and the usage comment in siara-cc/Unishox2 test_unishox2.c.
    // They line up with the declaration order of the static properties on CompressOptions.
    private static readonly (string Name, Func<CompressOption> Factory, int NativeOptionId, string[] SafeCases)[] OptionCases =
    {
        ("Default", () => CompressOptions.Default, 0, AsciiAndUnicodeCases),
        ("AlphaOnly", () => CompressOptions.AlphaOnly, 1, AlphaOnlyCases),
        ("AlphaNumOnly", () => CompressOptions.AlphaNumOnly, 2, AlphaNumCases),
        ("AlphaNumSymOnly", () => CompressOptions.AlphaNumSymOnly, 3, AlphaNumSymCases),
        ("AlphaNumSymOnlyText", () => CompressOptions.AlphaNumSymOnlyText, 4, AlphaNumSymCases),
        ("FavorAlpha", () => CompressOptions.FavorAlpha, 5, AsciiAndUnicodeCases),
        ("FavorDict", () => CompressOptions.FavorDict, 6, AsciiAndUnicodeCases),
        ("FavorSym", () => CompressOptions.FavorSym, 7, AsciiAndUnicodeCases),
        ("FavorUmlaut", () => CompressOptions.FavorUmlaut, 8, AsciiAndUnicodeCases),
        ("NoDict", () => CompressOptions.NoDict, 9, AsciiAndUnicodeCases),
        ("NoUnicode", () => CompressOptions.NoUnicode, 10, AlphaNumSymCases),
        ("NoUnicodeFavorText", () => CompressOptions.NoUnicodeFavorText, 11, AlphaNumSymCases),
        ("Url", () => CompressOptions.Url, 12, AsciiAndUnicodeCases),
        ("Json", () => CompressOptions.Json, 13, AsciiAndUnicodeCases),
        ("JsonNoUnicode", () => CompressOptions.JsonNoUnicode, 14, AlphaNumSymCases),
        ("Xml", () => CompressOptions.Xml, 15, AsciiAndUnicodeCases),
        ("Html", () => CompressOptions.Html, 16, AsciiAndUnicodeCases),
    };

    public static IEnumerable<object[]> OptionData => OptionCases.Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(OptionData))]
    public void AllOptions_RoundTripInCSharp(string presetName)
    {
        var (_, factory, _, cases) = OptionCases.Single(p => p.Name == presetName);
        var options = factory();
        foreach (string text in cases)
        {
            var compressed = Unishox2.Compress(text, options);
            Assert.Equal(text, Unishox2.Decompress(compressed, options));
        }
    }

    [SkippableFact]
    public void AllOptions_CrossDecompress_WhenNativeHarnessAvailable()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        var mismatches = new List<string>();
        int checkedCount = 0;

        foreach (var (name, factory, nativeOptionId, cases) in OptionCases)
        {
            var options = factory();
            foreach (string text in cases)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(text);
                string hexIn = ToHex(utf8);

                byte[] csRaw = Unishox2.Compress(text, options);

                string cCompressedHex = harness.Send($"C {nativeOptionId} {hexIn}");
                if (cCompressedHex == "ERR")
                {
                    mismatches.Add($"[{name}] C compress failed: {Describe(text)}");
                    continue;
                }
                byte[] cRaw = FromHex(cCompressedHex)!;


                string cDecompressedHex = harness.Send($"D {nativeOptionId} {ToHex(csRaw)}");
                if (cDecompressedHex == "ERR" || FromHex(cDecompressedHex) is not { } cDecoded || !cDecoded.SequenceEqual(utf8))
                {
                    mismatches.Add($"[{name}] C# -> C decompress mismatch: {Describe(text)}");
                    continue;
                }

                string csDecoded = Unishox2.Decompress(cRaw, options);
                if (csDecoded != text)
                {
                    mismatches.Add($"[{name}] C -> C# decompress mismatch: {Describe(text)}");
                    continue;
                }

                checkedCount++;
            }
        }

        Assert.True(checkedCount > 0, "The native harness was reported as available, but not a single case could be verified.");
        Assert.True(mismatches.Count == 0, $"{mismatches.Count} mismatches:\n" + string.Join("\n", mismatches.Take(30)));
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
