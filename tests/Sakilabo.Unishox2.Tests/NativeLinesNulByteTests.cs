// Decompresses, in C#, siara-cc/Unishox2 C data that contains NUL bytes.
// References past a NUL are incompatible, so the reverse direction is not required.

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

        // A self-reference within a single element (ctx=0). Because "AB\0" repeats periodically, whether a
        // match is found depends on whether the region past the NUL is included in the reference search.
        string element = RepeatWithEmbeddedNul("AB\0", 10);
        byte[] utf8 = Encoding.UTF8.GetBytes(element);
        string hexIn = ToHex(utf8);

        string compressedHex = harness.Send($"L 0 {hexIn}");
        Assert.NotEqual("ERR", compressedHex);
        byte[] cRaw = FromHex(compressedHex)!;

        // C to C# decompression, in the realistic pattern where the source text of the current element is
        // not retained. The bit sequence was encoded with lines, including the ctx=0 self-reference, so
        // decompression must also go through DecompressLines and DecompressLineChain. Decompress would use
        // the non-lines logic and interpret the bit sequence differently.
        byte[][] reconstructed = { cRaw };
        Assert.Equal(new[] { element }, Unishox2.DecompressLines(reconstructed));
    }

    [SkippableFact]
    public void CrossElementReferenceAcrossEmbeddedNul_UpstreamDataDecompresses_WhenAvailable()
    {
        using var harness = NativeHarness.TryCreate();
        NativeHarness.RequireAvailableOrSkip(harness);

        // The preceding element (ctx=1) contains a NUL. The reference search in siara-cc/Unishox2 is limited
        // by its strlen() to the part before the NUL, the two bytes "AB", so the content past it, the repeats
        // of "AB\0", is not searchable from the next element.
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

        // C to C# decompression. DecompressLines always treats data as lines, so this holds for the same reason as above.
        byte[][] reconstructed = { cRaw0, cRaw1 };
        Assert.Equal(elements, Unishox2.DecompressLines(reconstructed));
    }

    /// <summary>
    /// Even without a native harness, always verify at least that source text containing NUL is preserved
    /// in full, independently of any reference-search limits.
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
