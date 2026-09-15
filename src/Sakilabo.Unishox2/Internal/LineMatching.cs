using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal static class LineMatching
    {
        /// <summary>
        /// When compressing a single element, searches the already encoded range for a self-repetition.
        /// </summary>
        public static bool TryMatchOccurrence(byte[] input, int l, BitWriter w, ref HCodeGroup state, HCodes hCodes, out int newL)
        {
            int len = input.Length;
            int longestDist = 0;
            int longestLen = 0;
            for (int j = l - Tables.NiceLen; j >= 0; j--)
            {
                int k = l;
                while (k < len && j + k - l < l)
                {
                    if (input[k] != input[j + k - l])
                        break;
                    k++;
                }
                while (k < len && ((input[k] >> 6) == 2))
                    k--; // avoid a partial UTF-8 match
                if ((k - l) > (Tables.NiceLen - 1))
                {
                    int matchLen = k - l - Tables.NiceLen;
                    int matchDist = l - j - Tables.NiceLen + 1;
                    if (matchLen > longestLen)
                    {
                        longestLen = matchLen;
                        longestDist = matchDist;
                    }
                }
            }

            if (longestLen > 0)
            {
                w.AppendSwitchCode(state);
                w.AppendHCode(hCodes[HCodeGroup.Dictionary]);
                w.EncodeCount(longestLen);
                w.EncodeCount(longestDist);
                newL = l + (longestLen + Tables.NiceLen) - 1;
                return true;
            }

            newL = l;
            return false;
        }

        /// <summary>
        /// When compressing lines, searches for the longest match in the already encoded range of the
        /// current element and across the whole of each earlier element. ctx=0, the current element
        /// itself, is limited to limit=l.
        /// </summary>
        public static bool TryMatchLine(byte[] input, int l, BitWriter w, ILineChain chain, ref HCodeGroup state, HCodes hCodes, out int newL)
        {
            int len = input.Length;
            int lastOl = w.BitLength;
            int lastLen = 0;
            int lastDist = 0;
            int lastCtx = 0;
            int j = 0;

            for (int ctx = 0; ctx <= chain.MaxCtx; ctx++)
            {
                int lineLen = chain.GetLength(ctx);
                int limit = ctx == 0 ? l : lineLen;
                for (; j < limit; j++)
                {
                    int i = l;
                    int k = j;
                    while (k < lineLen && i < len)
                    {
                        if (chain.GetByte(ctx, k) != input[i])
                            break;
                        k++;
                        i++;
                    }
                    while (k > j && k < lineLen && ((chain.GetByte(ctx, k) >> 6) == 2))
                        k--; // avoid a partial UTF-8 match; k==lineLen is the end of the searchable range

                    if ((k - j) >= Tables.NiceLen)
                    {
                        if (lastLen != 0)
                        {
                            if (j > lastDist)
                                continue;
                            w.Rewind(lastOl);
                        }
                        lastLen = k - j;
                        lastDist = j;
                        lastCtx = ctx;
                        w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Dictionary]);
                        w.EncodeCount(lastLen - Tables.NiceLen);
                        w.EncodeCount(lastDist);
                        w.EncodeCount(lastCtx);
                        j += lastLen;
                    }
                }
            }

            if (lastLen != 0)
            {
                newL = l + lastLen - 1;
                return true;
            }

            newL = l;
            return false;
        }

        /// <summary>
        /// dist is the copy start position measured from the beginning of the element. Copying one byte
        /// at a time lets a self-reference whose source overlaps the output range keep reading the bytes
        /// that were just written.
        /// </summary>
        public static int? DecodeRepeat(BitReader r, ref int bitNo, List<byte> output, ILineChain chain)
        {
            long dictLen = r.ReadCount(ref bitNo) + Tables.NiceLen;
            if (dictLen < Tables.NiceLen)
                return null;
            long dist = r.ReadCount(ref bitNo);
            if (dist < 0)
                return null;
            long ctxRaw = r.ReadCount(ref bitNo);
            if (ctxRaw < 0)
                return null;
            int ctx = (int)ctxRaw;
            if (ctx > chain.MaxCtx)
                return null;

            int available = chain.GetLength(ctx);
            if (dist >= available)
                return null;

            for (long i = 0; i < dictLen; i++)
            {
                int srcIndex = (int)(dist + i);
                if (ctx == 0 && srcIndex >= chain.GetLength(0))
                    return null; // the self-reference reads past the settled range, which means the data is invalid
                output.Add(chain.GetByte(ctx, srcIndex));
            }

            return output.Count;
        }

        /// <summary>
        /// Copies a repetition within a single element. The search range constraints keep the source and the output from overlapping.
        /// </summary>
        public static int? DecodeOccurrence(BitReader r, ref int bitNo, List<byte> output)
        {
            long dictLen = r.ReadCount(ref bitNo) + Tables.NiceLen;
            if (dictLen < Tables.NiceLen)
                return null;
            long dist = r.ReadCount(ref bitNo) + Tables.NiceLen - 1;
            if (dist < Tables.NiceLen - 1)
                return null;
            if (output.Count - dist < 0)
                return null;

            for (long i = 0; i < dictLen; i++)
            {
                int srcIndex = (int)(output.Count - dist);
                output.Add(output[srcIndex]);
            }

            return output.Count;
        }
    }
}
