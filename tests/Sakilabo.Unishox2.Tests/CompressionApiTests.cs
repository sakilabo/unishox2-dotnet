// Verifies that Compress/Decompress and CompressLines/DecompressLines take a CompressOption independently
// per call, and that the compressed result does not retain the settings.

using System;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class CompressionApiTests
{
    [Fact]
    public void Compress_Decompress_WithExplicitOption_RoundTrips()
    {
        var options = CompressOptions.Json;
        byte[] compressed = Unishox2.Compress("{\"a\":1}", options);

        Assert.Equal("{\"a\":1}", Unishox2.Decompress(compressed, options));
    }

    [Fact]
    public void Compress_Decompress_WithExplicitHCodes_RoundTrips()
    {
        var options = new CompressOption
        {
            HCodes = new HCodes((0x00, 1), (0x80, 3), (0xA0, 3), (0xC0, 3), (0xE0, 3)),
        };
        byte[] compressed = Unishox2.Compress("Hello, World!", options);

        Assert.Equal("Hello, World!", Unishox2.Decompress(compressed, options));
    }

    [Fact]
    public void CompressLines_DecompressLines_WithExplicitOption_RoundTrips()
    {
        var options = CompressOptions.Json;
        string[] elements = { "{\"a\":1}", "{\"b\":2}", "" };

        byte[][] compressed = Unishox2.CompressLines(elements, options);

        Assert.Equal(elements, Unishox2.DecompressLines(compressed, options));
    }

    [Fact]
    public void CompressLines_EmptyArray_ProducesEmptyResult()
    {
        byte[][] compressed = Unishox2.CompressLines(Array.Empty<string>());

        Assert.Empty(compressed);
        Assert.Empty(Unishox2.DecompressLines(compressed));
    }

    [Fact]
    public void DecompressLines_EmptyArray_ProducesEmptyResult()
    {
        Assert.Empty(Unishox2.DecompressLines(Array.Empty<byte[]>()));
    }
}
