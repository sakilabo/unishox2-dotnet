// 定義済み設定の独立性と、変更した設定による往復を確認する。

using System.Linq;
using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class CompressOptionTests
{
    [Fact]
    public void CompressOptionsProperty_ClonesMutableValues()
    {
        var a = CompressOptions.Json;
        var b = CompressOptions.Json;

        Assert.NotSame(a.Templates, b.Templates);
        Assert.NotSame(a.FrequentSequences, b.FrequentSequences);

        a.Templates[0] = "mutated";
        a.FrequentSequences[0] = "mutated";
        Assert.NotEqual("mutated", b.Templates[0]);
        Assert.NotEqual("mutated", b.FrequentSequences[0]);

        var c = CompressOptions.Json;
        Assert.Equal(b.HCodes.ToArray(), c.HCodes.ToArray());
    }

    [Fact]
    public void ParameterlessConstructor_MatchesCompressOptionsDefault()
    {
        var a = new CompressOption();
        var b = CompressOptions.Default;

        Assert.Equal(a.HCodes.ToArray(), b.HCodes.ToArray());
        Assert.Equal(a.Templates, b.Templates);
        Assert.Equal(a.FrequentSequences.ToArray(), b.FrequentSequences.ToArray());
    }

    [Fact]
    public void DefaultTemplates_ContainsOnlyDefinedTemplates()
    {
        Assert.Equal(4, CompressOptions.Default.Templates.Length);
        Assert.All(CompressOptions.Default.Templates, Assert.NotNull);
    }

    [Fact]
    public void Templates_WithNullElement_Throws()
    {
        var options = CompressOptions.Default;
        Assert.Throws<ArgumentException>(() => options.Templates = new string[] { null! });
    }

    [Fact]
    public void Templates_WithMoreThanFiveElements_Throws()
    {
        var options = CompressOptions.Default;
        Assert.Throws<ArgumentException>(() => options.Templates = new[] { "a", "b", "c", "d", "e", "f" });
    }

    [Fact]
    public void Templates_WithEmptyElement_Throws()
    {
        var options = CompressOptions.Default;
        Assert.Throws<ArgumentException>(() => options.Templates = new[] { "" });
    }

    [Fact]
    public void HCodes_WithLengthGreaterThanEight_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HCodes((0, 9), null, null, null, null));
    }

    [Fact]
    public void HCodes_WithZeroLengthCode_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HCodes((0, 0), null, null, null, null));
    }

    [Fact]
    public void HCodes_ParameterlessConstructor_HasNoHorizontalCodes()
    {
        Assert.All(new HCodes(), Assert.Null);
    }

    [Fact]
    public void PropertyOverrides_TakePrecedenceOverBaseline_AndRoundTrip()
    {
        var options = CompressOptions.Default;
        options.HCodes = new HCodes((0x00, 1), (0x80, 3), (0xA0, 3), (0xC0, 3), (0xE0, 3));
        options.FrequentSequences = new[] { "aa", "bb", "cc", "dd", "ee", "ff" };
        options.Templates = new[] { "tfff-of-tf" };

        const string text = "Hello, World! aa bb 2026-09-14";
        byte[] compressed = Unishox2.Compress(text, options);

        Assert.Equal(text, Unishox2.Decompress(compressed, options));
    }
}
