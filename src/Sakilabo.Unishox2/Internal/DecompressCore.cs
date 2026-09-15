using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal static class DecompressCore
    {
        /// <summary>
        /// 圧縮バイト列を展開する。lineChain が null なら lines 機能なし(単独要素)として展開する。
        /// </summary>
        public static List<byte> Decompress(
            byte[] input,
            HCodes hCodes,
            byte[][] freqSeq,
            byte[][] templates,
            List<byte> output,
            ILineChain? lineChain)
        {
            var r = new BitReader(input);
            HCodeGroup dstate = HCodeGroup.Alpha;
            HCodeGroup h = HCodeGroup.Alpha;
            bool isAllUpper = false;
            int prevUni = 0;
            int bitNo = CompressCore.UnishoxMagicBitLen;
            int lenBits = r.LengthBits;

            while (bitNo < lenBits)
            {
                int origBitNo = bitNo;
                bool continueOuter = false;
                bool breakOuter = false;

                if (dstate == HCodeGroup.Delta || h == HCodeGroup.Delta)
                {
                    if (dstate != HCodeGroup.Delta)
                        h = dstate;
                    var unicode = r.ReadUnicode(ref bitNo);
                    if (!unicode.HasValue)
                        break;
                    var (isSpecial, specialCode, delta) = unicode.Value;
                    if (isSpecial)
                    {
                        switch (specialCode)
                        {
                            case 0:
                                output.Add((byte)' ');
                                continueOuter = true;
                                break;
                            case 1:
                                HCodeGroup? nextGroup = r.ReadHCodeIdx(ref bitNo, hCodes);
                                if (!nextGroup.HasValue)
                                {
                                    bitNo = lenBits;
                                    continueOuter = true;
                                    break;
                                }
                                h = nextGroup.Value;
                                if (h == HCodeGroup.Delta || h == HCodeGroup.Alpha)
                                {
                                    dstate = h;
                                    continueOuter = true;
                                    break;
                                }
                                if (h == HCodeGroup.Dictionary)
                                {
                                    int? rptRet = DecodeRepeatDispatch(r, ref bitNo, output, lineChain);
                                    if (rptRet == null)
                                        return output; // 現在までの出力で展開を終了する。
                                    h = dstate;
                                    continueOuter = true;
                                    break;
                                }
                                // switch だけを抜け、現在の入力位置から処理を続ける。
                                break;
                            case 2:
                                output.Add((byte)',');
                                continueOuter = true;
                                break;
                            case 3:
                                output.Add((byte)'.');
                                continueOuter = true;
                                break;
                            case 4:
                                output.Add(10);
                                continueOuter = true;
                                break;
                        }
                        if (continueOuter)
                            continue;
                    }
                    else
                    {
                        prevUni += (int)delta;
                        CoreHelpers.WriteUtf8(output, prevUni);
                    }

                    if (dstate == HCodeGroup.Delta && h == HCodeGroup.Delta)
                        continue;
                }
                else
                {
                    h = dstate;
                }

                char c = '\0';
                bool isUpper = isAllUpper;
                int? verticalCode = r.ReadVCodeIdx(ref bitNo);
                if (!verticalCode.HasValue)
                {
                    bitNo = origBitNo;
                    break;
                }
                int v = verticalCode.Value;

                bool fellThroughToOutput = true;

                if (v == 0 && h != HCodeGroup.Symbol)
                {
                    if (bitNo >= lenBits)
                        break;
                    if (h != HCodeGroup.Number || dstate != HCodeGroup.Delta)
                    {
                        HCodeGroup? nextGroup = r.ReadHCodeIdx(ref bitNo, hCodes);
                        if (!nextGroup.HasValue || bitNo >= lenBits)
                        {
                            bitNo = origBitNo;
                            break;
                        }
                        h = nextGroup.Value;
                    }
                    if (h == HCodeGroup.Alpha)
                    {
                        if (dstate == HCodeGroup.Alpha)
                        {
                            if (!hCodes[HCodeGroup.Alpha].HasValue &&
                                Tables.TermByteAlphaOnly == (byte)(r.Read8BitCode(bitNo - Tables.SwCodeLen) &
                                    (0xFF << (8 - (isAllUpper ? Tables.TermByteAlphaOnlyLenUpper : Tables.TermByteAlphaOnlyLenLower)))))
                            {
                                breakOuter = true;
                            }
                            else if (isAllUpper)
                            {
                                isUpper = false;
                                isAllUpper = false;
                                continue;
                            }
                            else
                            {
                                verticalCode = r.ReadVCodeIdx(ref bitNo);
                                if (!verticalCode.HasValue)
                                {
                                    bitNo = origBitNo;
                                    breakOuter = true;
                                }
                                else
                                {
                                    v = verticalCode.Value;
                                    if (v == 0)
                                    {
                                        HCodeGroup? nextGroup = r.ReadHCodeIdx(ref bitNo, hCodes);
                                        if (!nextGroup.HasValue)
                                        {
                                            bitNo = origBitNo;
                                            breakOuter = true;
                                        }
                                        else
                                        {
                                            h = nextGroup.Value;
                                            if (h == HCodeGroup.Alpha)
                                            {
                                                isAllUpper = true;
                                                continue;
                                            }
                                            isUpper = true;
                                        }
                                    }
                                    else
                                    {
                                        isUpper = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            dstate = HCodeGroup.Alpha;
                            continue;
                        }
                    }
                    else if (!breakOuter && h == HCodeGroup.Dictionary)
                    {
                        int? rptRet = DecodeRepeatDispatch(r, ref bitNo, output, lineChain);
                        if (rptRet == null)
                        {
                            breakOuter = true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (!breakOuter && h == HCodeGroup.Delta)
                    {
                        continue;
                    }
                    else if (!breakOuter)
                    {
                        verticalCode = h != HCodeGroup.Number || dstate != HCodeGroup.Delta
                            ? r.ReadVCodeIdx(ref bitNo)
                            : v;
                        if (!verticalCode.HasValue)
                        {
                            bitNo = origBitNo;
                            breakOuter = true;
                        }
                        else
                        {
                            v = verticalCode.Value;
                        }
                        if (!breakOuter && h == HCodeGroup.Number && v == 0)
                        {
                            fellThroughToOutput = false;
                            bool eof = false;
                            int? stepCode = r.GetStepCodeIdx(ref bitNo, 5);
                            int idx = stepCode.GetValueOrDefault();
                            if (!stepCode.HasValue)
                            {
                                breakOuter = true;
                            }
                            else if (idx == 0)
                            {
                                stepCode = r.GetStepCodeIdx(ref bitNo, 4);
                                if (!stepCode.HasValue || stepCode.Value >= 5)
                                {
                                    breakOuter = true;
                                }
                                else
                                {
                                    idx = stepCode.Value;
                                    long rem = r.ReadCount(ref bitNo);
                                    if (rem < 0 || idx >= templates.Length)
                                    {
                                        breakOuter = true;
                                    }
                                    else
                                    {
                                        byte[] template = templates[idx];
                                        int tlen = template.Length;
                                        if (rem > tlen)
                                        {
                                            breakOuter = true;
                                        }
                                        else
                                        {
                                            int remI = tlen - (int)rem;
                                            for (int j = 0; j < remI; j++)
                                            {
                                                byte cT = template[j];
                                                if (cT == (byte)'f' || cT == (byte)'r' || cT == (byte)'t' || cT == (byte)'o' || cT == (byte)'F')
                                                {
                                                    int nibbleLen = (cT == (byte)'f' || cT == (byte)'F') ? 4 : (cT == (byte)'r' ? 3 : (cT == (byte)'t' ? 2 : 1));
                                                    long rawChar = r.GetNumFromBits(bitNo, nibbleLen);
                                                    if (rawChar < 0)
                                                    {
                                                        eof = true;
                                                        break;
                                                    }
                                                    output.Add((byte)CoreHelpers.GetHexChar((int)rawChar, (cT == (byte)'f' || cT == (byte)'F') ? NibbleType.HexLower : NibbleType.HexUpper));
                                                    bitNo += nibbleLen;
                                                }
                                                else
                                                {
                                                    output.Add(cT);
                                                }
                                            }
                                            if (eof)
                                                breakOuter = true;
                                        }
                                    }
                                }
                            }
                            else if (idx == 5)
                            {
                                long binCount = r.ReadCount(ref bitNo);
                                if (binCount <= 0)
                                {
                                    breakOuter = true;
                                }
                                else
                                {
                                    do
                                    {
                                        long rawChar = r.GetNumFromBits(bitNo, 8);
                                        if (rawChar < 0)
                                            break;
                                        output.Add((byte)rawChar);
                                        bitNo += 8;
                                        binCount--;
                                    } while (binCount != 0);
                                    if (binCount > 0)
                                        breakOuter = true;
                                }
                            }
                            else
                            {
                                long nibbleCount;
                                if (idx == 2 || idx == 4)
                                {
                                    nibbleCount = 32;
                                }
                                else
                                {
                                    nibbleCount = r.ReadCount(ref bitNo);
                                    if (nibbleCount <= 0)
                                        breakOuter = true;
                                }
                                if (!breakOuter)
                                {
                                    do
                                    {
                                        long nibble = r.GetNumFromBits(bitNo, 4);
                                        if (nibble < 0)
                                            break;
                                        output.Add((byte)CoreHelpers.GetHexChar((int)nibble, idx < 3 ? NibbleType.HexLower : NibbleType.HexUpper));
                                        if ((idx == 2 || idx == 4) && (nibbleCount == 25 || nibbleCount == 21 || nibbleCount == 17 || nibbleCount == 13))
                                            output.Add((byte)'-');
                                        bitNo += 4;
                                        nibbleCount--;
                                    } while (nibbleCount != 0);
                                    if (nibbleCount > 0)
                                        breakOuter = true;
                                }
                            }

                            if (!breakOuter)
                            {
                                if (dstate == HCodeGroup.Delta)
                                    h = HCodeGroup.Delta;
                                continue;
                            }
                        }
                    }
                }

                if (breakOuter)
                    break;

                if (!fellThroughToOutput)
                    continue;

                if (isUpper && v == 1)
                {
                    h = dstate = HCodeGroup.Delta;
                    continue;
                }

                if ((int)h < 3 && v < 28)
                    c = (char)Tables.UsxSets[(int)h][v];

                if (c >= 'a' && c <= 'z')
                {
                    dstate = HCodeGroup.Alpha;
                    if (isUpper)
                        c = (char)(c - 32);
                }
                else
                {
                    if (c >= '0' && c <= '9')
                    {
                        dstate = HCodeGroup.Number;
                    }
                    else if (c == 0)
                    {
                        bool handled = true;
                        if (v == 8)
                        {
                            output.Add((byte)'\r');
                            output.Add((byte)'\n');
                        }
                        else if (h == HCodeGroup.Number && v == 26)
                        {
                            long count = r.ReadCount(ref bitNo);
                            if (count < 0)
                            {
                                break;
                            }
                            count += 4;
                            if (output.Count <= 0)
                                return output; // 不正な符号では現在までの出力を返す。
                            byte rptC = output[output.Count - 1];
                            while (count-- > 0)
                                output.Add(rptC);
                        }
                        else if (h == HCodeGroup.Symbol && v > 24)
                        {
                            int fi = v - 25;
                            AppendFreqSeq(output, freqSeq, fi);
                        }
                        else if (h == HCodeGroup.Number && v > 22 && v < 26)
                        {
                            int fi = v - (23 - 3);
                            AppendFreqSeq(output, freqSeq, fi);
                        }
                        else
                        {
                            handled = false;
                        }

                        if (!handled)
                            break; // Terminator

                        if (dstate == HCodeGroup.Delta)
                            h = HCodeGroup.Delta;
                        continue;
                    }
                }

                if (dstate == HCodeGroup.Delta)
                    h = HCodeGroup.Delta;
                output.Add((byte)c);
            }

            return output;
        }

        private static void AppendFreqSeq(List<byte> output, byte[][] freqSeq, int index)
        {
            if (index < 0 || index >= freqSeq.Length)
                throw new UnishoxFormatException("圧縮データが未設定の頻出文字列を参照しています。");
            byte[] seq = freqSeq[index];
            for (int i = 0; i < seq.Length; i++)
                output.Add(seq[i]);
        }

        private static int? DecodeRepeatDispatch(BitReader r, ref int bitNo, List<byte> output, ILineChain? lineChain)
        {
            return lineChain != null
                ? LineMatching.DecodeRepeat(r, ref bitNo, output, lineChain)
                : LineMatching.DecodeOccurrence(r, ref bitNo, output);
        }
    }
}
