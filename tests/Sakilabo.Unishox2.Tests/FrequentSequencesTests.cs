// Checks the element count and contents of CompressOption.FrequentSequences.

using System;
using System.Collections.Generic;
using System.Linq;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class FrequentSequenceOptionTests
{
    [Fact]
    public void Property_AllowsFewerThanSixElements()
    {
        var options = new CompressOption { FrequentSequences = new[] { "あ", "い" } };

        Assert.Equal(new[] { "あ", "い" }, options.FrequentSequences);
    }

    [Fact]
    public void Property_RejectsMoreThanSixElements()
    {
        var options = new CompressOption();

        Assert.Throws<ArgumentException>(() => options.FrequentSequences = new[] { "1", "2", "3", "4", "5", "6", "7" });
    }

    [Fact]
    public void Property_RejectsNullElement()
    {
        var options = new CompressOption();

        Assert.Throws<ArgumentException>(() => options.FrequentSequences = new string[] { "1", null! });
    }

    [Fact]
    public void Property_RejectsEmptyElement()
    {
        var options = new CompressOption();

        Assert.Throws<ArgumentException>(() => options.FrequentSequences = new[] { "1", "" });
    }

    [Fact]
    public void MissingSequence_IsSkippedDuringCompression()
    {
        var options = new CompressOption
        {
            FrequentSequences = new[] { " the " },
        };
        const string text = " the value and the value";

        Assert.Equal(text, Unishox2.Decompress(Unishox2.Compress(text, options), options));
    }

    [Fact]
    public void MissingSequence_ReferencedByCompressedData_Throws()
    {
        var compressOptions = new CompressOption
        {
            FrequentSequences = new[] { "xyzxyz" },
        };
        var decompressOptions = new CompressOption
        {
            FrequentSequences = Array.Empty<string>(),
        };
        byte[] compressed = Unishox2.Compress("xyzxyz", compressOptions);

        Assert.Throws<UnishoxFormatException>(() => Unishox2.Decompress(compressed, decompressOptions));
    }
}
