using System;
using System.Collections.Generic;
using System.Text;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// A validated copy of a CompressOption, prepared for the compression and decompression routines.
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

        /// <summary>The frequent sequences as UTF-8 byte sequences, at most 6 elements.</summary>
        public byte[][] FreqSeq { get; }

        /// <summary>The template strings as UTF-8 byte sequences, at most 5 elements.</summary>
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
                throw new ArgumentException("Templates must have 5 or fewer elements.", nameof(CompressOption.Templates));
            for (int i = 0; i < templates.Length; i++)
            {
                if (string.IsNullOrEmpty(templates[i]))
                    throw new ArgumentException("Templates must not contain null or empty elements.", nameof(CompressOption.Templates));
            }
            return ToUtf8Array(templates);
        }

        private static byte[][] ValidateFrequentSequences(string[] sequences)
        {
            if (sequences.Length > 6)
                throw new ArgumentException("FrequentSequences must have 6 or fewer elements.", nameof(CompressOption.FrequentSequences));
            for (int i = 0; i < sequences.Length; i++)
            {
                if (string.IsNullOrEmpty(sequences[i]))
                    throw new ArgumentException("FrequentSequences must not contain null or empty elements.", nameof(CompressOption.FrequentSequences));
            }
            return ToUtf8Array(sequences);
        }

        /// <summary>
        /// Horizontal code lengths are limited to the 0 to 8 bits that a single write can handle.
        /// </summary>
        /// <summary>
        /// Verifies that the horizontal codes form a uniquely decodable prefix code.
        /// When ALPHA has no horizontal code every character is treated as ALPHA, so no other group may have one either.
        /// </summary>
        private static void ValidatePrefixFree(HCodes hCodes)
        {
            if (!hCodes[HCodeGroup.Alpha].HasValue)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (hCodes[i].HasValue)
                        throw new ArgumentException("When ALPHA has no horizontal code, no other group may have one either.", nameof(CompressOption.HCodes));
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
                            $"The codes for HCodes groups {i} and {j} collide as prefixes " +
                            "(the code set is not uniquely decodable).",
                            nameof(CompressOption.HCodes));
                    }
                }
            }
        }
    }
}
