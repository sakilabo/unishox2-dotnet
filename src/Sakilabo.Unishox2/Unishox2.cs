using System;
using System.Collections.Generic;
using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// Unishox2 による文字列圧縮・展開の入口。CompressOption は呼び出しごとに指定し、
    /// 圧縮結果(byte[]/byte[][])には保持しない。
    /// </summary>
    public static class Unishox2
    {
        /// <summary>文字列を、指定した設定(省略時は既定設定)で圧縮する。</summary>
        public static byte[] Compress(string input, CompressOption? options = null)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            byte[] utf8 = Utf8Strict.GetBytesStrict(input);
            return CompressSingle(utf8, options);
        }

        /// <summary>UTF-8 バイト列を、指定した設定(省略時は既定設定)で圧縮する。</summary>
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
        /// 文字列の配列を lines として圧縮する。各要素は、Unishox2 の lines 機能で前の要素を
        /// 参照しながら圧縮する。空配列は 0 要素の結果を返す。
        /// </summary>
        public static byte[][] CompressLines(string[] input, CompressOption? options = null)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            var utf8Elements = new byte[input.Length][];
            for (int i = 0; i < input.Length; i++)
            {
                string element = input[i] ?? throw new ArgumentException("input に null 要素を含めることはできません。", nameof(input));
                utf8Elements[i] = Utf8Strict.GetBytesStrict(element);
            }
            return CompressLinesCore(utf8Elements, options);
        }

        /// <summary>
        /// UTF-8 バイト列の配列を lines として圧縮する。各要素は、Unishox2 の lines 機能で前の要素を
        /// 参照しながら圧縮する。空配列は 0 要素の結果を返す。
        /// </summary>
        public static byte[][] CompressLines(byte[][] utf8Elements, CompressOption? options = null)
        {
            if (utf8Elements == null)
                throw new ArgumentNullException(nameof(utf8Elements));
            var validated = new byte[utf8Elements.Length][];
            for (int i = 0; i < utf8Elements.Length; i++)
            {
                byte[] element = utf8Elements[i] ?? throw new ArgumentException("utf8Elements に null 要素を含めることはできません。", nameof(utf8Elements));
                Utf8Strict.ValidateStrict(element);
                validated[i] = element;
            }
            return CompressLinesCore(validated, options);
        }

        private static byte[][] CompressLinesCore(byte[][] elements, CompressOption? options)
        {
            var settings = EffectiveSettings.Resolve(options);
            var compressedParts = new byte[elements.Length][];
            var previousElements = new List<byte[]>(); // index 0 が直前の要素
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
        /// Unishox2 形式の圧縮バイト列 1 件(<see cref="Compress(string, CompressOption?)"/> 等、
        /// lines ではない圧縮の結果)を、指定した設定(省略時は既定設定)で元の文字列へ展開する。
        /// options は圧縮時に渡したものと同じ内容を渡すこと。
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
        /// Unishox2 形式の圧縮バイト列の配列(<see cref="CompressLines(string[], CompressOption?)"/> 等の結果)を、
        /// 指定した設定(省略時は既定設定)で要素ごとの文字列の配列へ展開する。
        /// options は圧縮時に渡したものと同じ内容を渡すこと。
        /// </summary>
        public static string[] DecompressLines(byte[][] data, CompressOption? options = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var settings = EffectiveSettings.Resolve(options);
            var decodedElements = new List<byte[]>(); // index 0 が直前の要素
            var result = new string[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] element = data[i] ?? throw new ArgumentException("data に null 要素を含めることはできません。", nameof(data));
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
