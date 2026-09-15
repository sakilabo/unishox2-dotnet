using Sakilabo.Unishox2;

namespace Sakilabo.Unishox2.Tests;

public class RoundTripTests
{
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Hello, World!")]
    [InlineData("The quick brown fox jumps over the lazy dog")]
    [InlineData("THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG")]
    [InlineData("1234567890")]
    [InlineData("こんにちは世界")]
    [InlineData("日本語とEnglishの混在テキストです。123")]
    [InlineData("😀😃😄 絵文字テスト 🎉")]
    [InlineData("line1\nline2\r\nline3\rline4\ttabbed")]
    [InlineData("{\"key\": \"value\", \"num\": 123}")]
    [InlineData("https://example.com/path?query=1")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("2026-09-14T12:34:56.789Z")]
    [InlineData("deadbeefCAFEBABE0123456789abcdef")]
    [InlineData("AAAAAAAAAAAAAAAA")]
    [InlineData("ABABABABABABABABABABABABABABABABABABAB")]
    [InlineData("XAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXAXA")]
    public void CompressDecompress_ReturnsOriginal(string input)
    {
        var compressed = Unishox2.Compress(input);
        var decompressed = Unishox2.Decompress(compressed);
        Assert.Equal(input, decompressed);
    }

    [Fact]
    public void Compress_ByteArrayInput_RoundTrips()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("byte array input test");
        var compressed = Unishox2.Compress(bytes);
        var decompressed = Unishox2.Decompress(compressed);
        Assert.Equal("byte array input test", decompressed);
    }

    [Fact]
    public void Compress_NullString_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => Unishox2.Compress((string)null!));
    }

    [Fact]
    public void Decompress_NullData_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => Unishox2.Decompress(null!));
    }
}
