using System;
using System.Collections.Generic;
using System.Text;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// CompressOption を検証し、圧縮・展開処理用にコピーした設定。
    /// </summary>
    internal sealed class EffectiveSettings
    {
        public EffectiveSettings(HCodes hCodes, byte[][] freqSeq, byte[][] templates)
        {
            HCodes = hCodes;
            FreqSeq = freqSeq;
            Templates = templates;
        }

        public HCodes HCodes { get; }

        /// <summary>頻出文字列を UTF-8 バイト列化したもの(最大6要素)。</summary>
        public byte[][] FreqSeq { get; }

        /// <summary>テンプレート文字列を UTF-8 バイト列化したもの(最大 5 要素)。</summary>
        public byte[][] Templates { get; }

        public static EffectiveSettings Resolve(CompressOption? options)
        {
            options ??= CompressOptions.Default;

            byte[][] freqSeq = ValidateFrequentSequences(options.FrequentSequences);
            byte[][] templates = ValidateTemplates(options.Templates);

            ValidatePrefixFree(options.HCodes);

            return new EffectiveSettings(options.HCodes, freqSeq, templates);
        }

        private static byte[][] ToUtf8Array(IReadOnlyList<string> source)
        {
            var result = new byte[source.Count][];
            for (int i = 0; i < source.Count; i++)
                result[i] = Encoding.UTF8.GetBytes(source[i]);
            return result;
        }

        private static byte[][] ValidateTemplates(string[] templates)
        {
            if (templates.Length > 5)
                throw new ArgumentException("Templates は 5 要素以下である必要があります。", nameof(CompressOption.Templates));
            for (int i = 0; i < templates.Length; i++)
            {
                if (string.IsNullOrEmpty(templates[i]))
                    throw new ArgumentException("Templates に null または空文字列は指定できません。", nameof(CompressOption.Templates));
            }
            return ToUtf8Array(templates);
        }

        private static byte[][] ValidateFrequentSequences(string[] sequences)
        {
            if (sequences.Length > 6)
                throw new ArgumentException("FrequentSequences は 6 要素以下である必要があります。", nameof(CompressOption.FrequentSequences));
            for (int i = 0; i < sequences.Length; i++)
            {
                if (string.IsNullOrEmpty(sequences[i]))
                    throw new ArgumentException("FrequentSequences に null または空文字列は指定できません。", nameof(CompressOption.FrequentSequences));
            }
            return ToUtf8Array(sequences);
        }

        /// <summary>
        /// 水平符号長は 1 回の書き込みで扱える 0～8 ビットに制限する。
        /// </summary>
        /// <summary>
        /// 水平符号が一意に判別できる接頭辞符号になっていることを検証する。
        /// ALPHA の水平符号がない場合は常に ALPHA として扱うため、他のグループにも水平符号を設定できない。
        /// </summary>
        private static void ValidatePrefixFree(HCodes hCodes)
        {
            if (!hCodes[HCodeGroup.Alpha].HasValue)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (hCodes[i].HasValue)
                        throw new ArgumentException("ALPHA の水平符号がない場合、他のグループにも水平符号は設定できません。", nameof(CompressOption.HCodes));
                }
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                if (!hCodes[i].HasValue)
                    continue;
                for (int j = i + 1; j < 5; j++)
                {
                    if (!hCodes[j].HasValue)
                        continue;
                    (byte Code, byte Length) first = hCodes[i]!.Value;
                    (byte Code, byte Length) second = hCodes[j]!.Value;
                    int minLen = Math.Min(first.Length, second.Length);
                    byte mask = Tables.UsxMask[minLen - 1];
                    if ((first.Code & mask) == (second.Code & mask))
                    {
                        throw new ArgumentException(
                            $"HCodes のグループ {i} と {j} の符号が接頭辞として衝突しています" +
                            "(一意に復号できる符号体系ではありません)。",
                            nameof(CompressOption.HCodes));
                    }
                }
            }
        }
    }
}
