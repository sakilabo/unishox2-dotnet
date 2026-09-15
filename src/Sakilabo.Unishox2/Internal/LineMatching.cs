using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal static class LineMatching
    {
        /// <summary>
        /// 単一要素の圧縮時、既にエンコード済みの範囲内から自己反復を探す。
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
                    k--; // UTF-8 の部分一致を避ける
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
        /// lines 圧縮時、現在要素の既エンコード範囲および
        /// 過去の要素全体から最長一致を探す。ctx=0(現在要素自身)は limit=l に制限される。
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
                        k--; // UTF-8 の部分一致を避ける(k==lineLen は参照検索の対象範囲の終端)

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
        /// dist は要素先頭からのコピー開始位置。1 バイトずつコピーするため、コピー元と
        /// 出力範囲が重なる自己参照も、直前に出力したバイトを続けて参照できる。
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
                    return null; // 自己参照が確定済みの範囲を超えて先読みしようとしている(不正なデータ)
                output.Add(chain.GetByte(ctx, srcIndex));
            }

            return output.Count;
        }

        /// <summary>
        /// 単一要素の反復をコピーする。検索範囲の制約によりコピー元と出力範囲は重ならない。
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
