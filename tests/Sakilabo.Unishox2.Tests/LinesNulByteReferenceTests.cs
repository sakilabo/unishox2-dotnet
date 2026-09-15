// Verifies that the lines feature treats NUL as ordinary data.
// NativeLinesNulByteTests.cs checks that Sakilabo.Unishox2 correctly decompresses data produced by the
// siara-cc/Unishox2 C implementation. This file instead calls the internal TryMatchLine directly to check
// that Sakilabo.Unishox2 itself includes the region past a NUL in the reference search, because a plain
// round-trip cannot show whether the search actually crossed or passed a NUL.

using System.Linq;
using System.Text;
using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2.Tests;

public class LinesNulByteReferenceTests
{
    /// <summary>
    /// A self-reference within the current element (ctx=0). Of the 16 bytes formed by repeating
    /// "AB\0CDEFG" twice, the second copy (l=8..15) matches the whole of the first (0..7). That match
    /// spans the NUL at index 2, so this directly confirms that the reference search finds matches across a NUL.
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
    /// The preceding element (ctx=1) is "X\0HELLO!", where the only match candidate ("HELLO!") lies past
    /// the NUL at index 1. Truncating at strlen() would leave just the single byte "X" visible in previous,
    /// making the match impossible to find. Because GetLength returns the full length, this directly confirms
    /// that the region past a NUL is also searched.
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
    /// Confirms through the public API that an overlapping self-reference copy, a short-period repetition,
    /// still round-trips across a NUL, that is, that the byte-at-a-time copy in DecodeRepeat holds with NUL included.
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
    /// A round-trip where the next element references content past the NUL of the preceding element ("HELLOWORLD").
    /// </summary>
    [Fact]
    public void CrossElementReference_ToContentAfterEmbeddedNul_RoundTrips()
    {
        string[] elements = { "X\0HELLOWORLD", "HELLOWORLD" };

        var compressed = Unishox2.CompressLines(elements);
        Assert.Equal(elements, Unishox2.DecompressLines(compressed));
    }

    /// <summary>
    /// A self-reference round-trip for an element mixing multi-byte UTF-8 characters and NUL. Confirms that
    /// the avoidance of partial UTF-8 matches, the continuation-byte rewind in TryMatchLine, still respects
    /// character boundaries over the extended range that spans a NUL.
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
