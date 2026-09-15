using System;
using System.Collections.Generic;
using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// Entry point for Unishox2 string compression and decompression. A CompressOption is supplied
    /// per call and is not retained in the compressed result (byte[]/byte[][]).
    /// </summary>
    public static class Unishox2
    {
        /// <summary>Compresses a string using the given options, or the default options when omitted.</summary>
        public static byte[] Compress(string input, CompressOption? options = null)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            byte[] utf8 = Utf8Strict.GetBytesStrict(input);
            return CompressSingle(utf8, options);
        }

        /// <summary>Compresses a UTF-8 byte sequence using the given options, or the default options when omitted.</summary>
        public static byte[] Compress(byte[] utf8Bytes, CompressOption? options = null)
        {
            if (utf8Bytes == null)
                throw new ArgumentNullException(nameof(utf8Bytes));
            Utf8Strict.ValidateStrict(utf8Bytes);
            return CompressSingle(utf8Bytes, options);
        }

        private static byte[] CompressSingle(byte[] utf8Bytes, CompressOption? options)
        {
            var settings = EffectiveSettings.Resolve(options);
            var compressed = CompressCore.Compress(utf8Bytes, settings.HCodes, settings.FreqSeq, settings.Templates, null);
            return compressed.ToArray();
        }

        /// <summary>
        /// Compresses an array of strings as lines. Each element is compressed while referring to the
        /// preceding elements through the Unishox2 lines feature. An empty array yields a zero-element result.
        /// </summary>
        public static byte[][] CompressLines(string[] input, CompressOption? options = null)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            var utf8Elements = new byte[input.Length][];
            for (int i = 0; i < input.Length; i++)
            {
                string element = input[i] ?? throw new ArgumentException("input must not contain null elements.", nameof(input));
                utf8Elements[i] = Utf8Strict.GetBytesStrict(element);
            }
            return CompressLinesCore(utf8Elements, options);
        }

        /// <summary>
        /// Compresses an array of UTF-8 byte sequences as lines. Each element is compressed while referring to
        /// the preceding elements through the Unishox2 lines feature. An empty array yields a zero-element result.
        /// </summary>
        public static byte[][] CompressLines(byte[][] utf8Elements, CompressOption? options = null)
        {
            if (utf8Elements == null)
                throw new ArgumentNullException(nameof(utf8Elements));
            var validated = new byte[utf8Elements.Length][];
            for (int i = 0; i < utf8Elements.Length; i++)
            {
                byte[] element = utf8Elements[i] ?? throw new ArgumentException("utf8Elements must not contain null elements.", nameof(utf8Elements));
                Utf8Strict.ValidateStrict(element);
                validated[i] = element;
            }
            return CompressLinesCore(validated, options);
        }

        private static byte[][] CompressLinesCore(byte[][] elements, CompressOption? options)
        {
            var settings = EffectiveSettings.Resolve(options);
            var compressedParts = new byte[elements.Length][];
            var previousElements = new List<byte[]>(); // index 0 holds the immediately preceding element
            for (int i = 0; i < elements.Length; i++)
            {
                var chain = new CompressLineChain(elements[i], previousElements);
                var compressed = CompressCore.Compress(elements[i], settings.HCodes, settings.FreqSeq, settings.Templates, chain);
                compressedParts[i] = compressed.ToArray();
                previousElements.Insert(0, elements[i]);
            }
            return compressedParts;
        }

        /// <summary>
        /// Decompresses a single Unishox2 compressed byte sequence (the result of
        /// <see cref="Compress(string, CompressOption?)"/> and friends, not of a lines compression)
        /// back into the original string, using the given options or the default options when omitted.
        /// The options must match those passed at compression time.
        /// </summary>
        public static string Decompress(byte[] data, CompressOption? options = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var settings = EffectiveSettings.Resolve(options);
            var output = new List<byte>();
            DecompressCore.Decompress(data, settings.HCodes, settings.FreqSeq, settings.Templates, output, null);
            return Utf8Strict.GetStringStrict(output.ToArray());
        }

        /// <summary>
        /// Decompresses an array of Unishox2 compressed byte sequences (the result of
        /// <see cref="CompressLines(string[], CompressOption?)"/> and friends) into a string per element,
        /// using the given options or the default options when omitted.
        /// The options must match those passed at compression time.
        /// </summary>
        public static string[] DecompressLines(byte[][] data, CompressOption? options = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var settings = EffectiveSettings.Resolve(options);
            var decodedElements = new List<byte[]>(); // index 0 holds the immediately preceding element
            var result = new string[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] element = data[i] ?? throw new ArgumentException("data must not contain null elements.", nameof(data));
                var output = new List<byte>();
                var chain = new DecompressLineChain(output, decodedElements);
                DecompressCore.Decompress(element, settings.HCodes, settings.FreqSeq, settings.Templates, output, chain);
                byte[] elementBytes = output.ToArray();
                result[i] = Utf8Strict.GetStringStrict(elementBytes);
                decodedElements.Insert(0, elementBytes);
            }
            return result;
        }
    }
}
