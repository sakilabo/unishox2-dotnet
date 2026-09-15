using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal static class CompressCore
    {
        public const byte UnishoxMagicBits = 0xFF;
        public const int UnishoxMagicBitLen = 1;

        public static List<byte> Compress(
            byte[] input,
            HCodes hCodes,
            byte[][] freqSeq,
            byte[][] templates,
            ILineChain? lineChain)
        {
            var w = new BitWriter();
            HCodeGroup state = HCodeGroup.Alpha;
            bool isAllUpper = false;
            int prevUni = 0;
            int len = input.Length;

            w.AppendBits(UnishoxMagicBits, UnishoxMagicBitLen);

            int l = 0;
            while (l < len)
            {
                if (hCodes[HCodeGroup.Dictionary].HasValue && l < len - Tables.NiceLen + 1)
                {
                    bool matched;
                    int newL;
                    if (lineChain != null)
                        matched = LineMatching.TryMatchLine(input, l, w, lineChain, ref state, hCodes, out newL);
                    else
                        matched = LineMatching.TryMatchOccurrence(input, l, w, ref state, hCodes, out newL);
                    l = newL;
                    if (matched)
                    {
                        l++;
                        continue;
                    }
                }

                byte cInByte = input[l];
                char cIn = (char)cInByte;

                // RPT_CODE: the same character repeated 4 or more times
                if (l != 0 && len > 4 && l < len - 4 && hCodes[HCodeGroup.Number].HasValue)
                {
                    if (cInByte == input[l - 1] && cInByte == input[l + 1] && cInByte == input[l + 2] && cInByte == input[l + 3])
                    {
                        int rptCount = l + 4;
                        while (rptCount < len && input[rptCount] == cInByte)
                            rptCount++;
                        rptCount -= l;
                        w.AppendCode(Tables.RptCode, ref state, hCodes);
                        w.EncodeCount(rptCount - 4);
                        l += rptCount;
                        continue;
                    }
                }

                // UUID (GUID) detection: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx, 36 characters
                if (l <= len - 36 && hCodes[HCodeGroup.Number].HasValue)
                {
                    if (input[l + 8] == '-' && input[l + 13] == '-' && input[l + 18] == '-' && input[l + 23] == '-')
                    {
                        NibbleType hexType = NibbleType.Num;
                        int uidPos = l;
                        for (; uidPos < l + 36; uidPos++)
                        {
                            char cUid = (char)input[uidPos];
                            // Check for hyphens by position relative to the start of the UUID.
                            int relPos = uidPos - l;
                            if (cUid == '-' && (relPos == 8 || relPos == 13 || relPos == 18 || relPos == 23))
                                continue;
                            NibbleType nibType = CoreHelpers.GetNibbleType(cUid);
                            if (nibType == NibbleType.Not)
                                break;
                            if (nibType != NibbleType.Num)
                            {
                                if (hexType != NibbleType.Num && hexType != nibType)
                                    break;
                                hexType = nibType;
                            }
                        }
                        if (uidPos == l + 36)
                        {
                            w.AppendNibbleEscape(state, hCodes);
                            w.AppendBits(hexType == NibbleType.HexLower ? (byte)0xC0 : (byte)0xF0, hexType == NibbleType.HexLower ? 3 : 5);
                            for (uidPos = l; uidPos < l + 36; uidPos++)
                            {
                                char cUid = (char)input[uidPos];
                                if (cUid != '-')
                                    w.AppendBits(CoreHelpers.GetBaseCode(cUid), 4);
                            }
                            l += 36;
                            continue;
                        }
                    }
                }

                // HEX detection: a run of 4 or more hexadecimal digits
                if (l < len - 5 && hCodes[HCodeGroup.Number].HasValue)
                {
                    NibbleType hexType = NibbleType.Num;
                    int hexLen = 0;
                    do
                    {
                        NibbleType nibType = CoreHelpers.GetNibbleType((char)input[l + hexLen]);
                        if (nibType == NibbleType.Not)
                            break;
                        if (nibType != NibbleType.Num)
                        {
                            if (hexType != NibbleType.Num && hexType != nibType)
                                break;
                            hexType = nibType;
                        }
                        hexLen++;
                    } while (l + hexLen < len);
                    if (hexLen > 10 && hexType == NibbleType.Num)
                        hexType = NibbleType.HexLower;
                    if ((hexType == NibbleType.HexLower || hexType == NibbleType.HexUpper) && hexLen > 3)
                    {
                        w.AppendNibbleEscape(state, hCodes);
                        w.AppendBits(hexType == NibbleType.HexLower ? (byte)0x80 : (byte)0xE0, hexType == NibbleType.HexLower ? 2 : 4);
                        w.EncodeCount(hexLen);
                        int remaining = hexLen;
                        int p = l;
                        do
                        {
                            w.AppendBits(CoreHelpers.GetBaseCode((char)input[p]), 4);
                            p++;
                        } while (--remaining != 0);
                        l = p;
                        continue;
                    }
                }

                // Template match. Template strings are UTF-8 byte sequences; 'f', 'F', 'r', 't' and 'o'
                // are single ASCII bytes, so they never collide with UTF-8 continuation bytes (0x80 and above).
                if (templates.Length != 0)
                {
                    int matchedTemplate = -1;
                    for (int ti = 0; ti < templates.Length; ti++)
                    {
                        byte[] template = templates[ti];
                        int rem = template.Length;
                        int j = 0;
                        for (; j < rem && l + j < len; j++)
                        {
                            byte cT = template[j];
                            byte cInT = input[l + j];
                            if (cT == (byte)'f' || cT == (byte)'F')
                            {
                                var nib = CoreHelpers.GetNibbleType((char)cInT);
                                if (nib != (cT == (byte)'f' ? NibbleType.HexLower : NibbleType.HexUpper) && nib != NibbleType.Num)
                                    break;
                            }
                            else if (cT == (byte)'r' || cT == (byte)'t' || cT == (byte)'o')
                            {
                                byte maxDigit = cT == (byte)'r' ? (byte)'7' : (cT == (byte)'t' ? (byte)'3' : (byte)'1');
                                if (cInT < (byte)'0' || cInT > maxDigit)
                                    break;
                            }
                            else if (cT != cInT)
                            {
                                break;
                            }
                        }
                        if ((float)j / rem > 0.66f)
                        {
                            int rem2 = rem - j;
                            w.AppendNibbleEscape(state, hCodes);
                            w.AppendBits(0, 1);
                            w.AppendBits((byte)(Tables.CountCodes[ti] & 0xF8), Tables.CountCodes[ti] & 0x07);
                            w.EncodeCount(rem2);
                            for (int k = 0; k < j; k++)
                            {
                                byte cT = template[k];
                                if (cT == (byte)'f' || cT == (byte)'F')
                                {
                                    w.AppendBits(CoreHelpers.GetBaseCode((char)input[l + k]), 4);
                                }
                                else if (cT == (byte)'r' || cT == (byte)'t' || cT == (byte)'o')
                                {
                                    int bits = cT == (byte)'r' ? 3 : (cT == (byte)'t' ? 2 : 1);
                                    w.AppendBits((byte)((input[l + k] - '0') << (8 - bits)), bits);
                                }
                            }
                            l += j;
                            matchedTemplate = ti;
                            break;
                        }
                    }
                    if (matchedTemplate >= 0)
                        continue;
                }

                // Frequent sequences, compared as UTF-8 byte sequences
                int matchedFreq = -1;
                for (int fi = 0; fi < freqSeq.Length; fi++)
                {
                    byte[] seq = freqSeq[fi];
                    int seqLen = seq.Length;
                    if (len - seqLen >= 0 && l <= len - seqLen && hCodes[Tables.UsxFreqCodes[fi] >> 5].HasValue)
                    {
                        bool eq = true;
                        for (int si = 0; si < seqLen; si++)
                        {
                            if (seq[si] != input[l + si])
                            {
                                eq = false;
                                break;
                            }
                        }
                        if (eq)
                        {
                            w.AppendCode(Tables.UsxFreqCodes[fi], ref state, hCodes);
                            l += seqLen;
                            matchedFreq = fi;
                            break;
                        }
                    }
                }
                if (matchedFreq >= 0)
                    continue;

                bool isUpper = cIn >= 'A' && cIn <= 'Z';
                if (!isUpper && isAllUpper)
                {
                    isAllUpper = false;
                    w.AppendSwitchCode(state);
                    w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                    state = HCodeGroup.Alpha;
                }
                if (isUpper && !isAllUpper)
                {
                    if (state == HCodeGroup.Number)
                    {
                        w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                        state = HCodeGroup.Alpha;
                    }
                    w.AppendSwitchCode(state);
                    w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                    if (state == HCodeGroup.Delta)
                    {
                        state = HCodeGroup.Alpha;
                        w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                    }
                }

                char cNext = l + 1 < len ? (char)input[l + 1] : '\0';

                if (cIn >= 32 && cIn <= 126)
                {
                    if (isUpper && !isAllUpper)
                    {
                        int ll;
                        for (ll = l + 4; ll >= l && ll < len; ll--)
                        {
                            char cUp = (char)input[ll];
                            if (cUp < 'A' || cUp > 'Z')
                                break;
                        }
                        if (ll == l - 1)
                        {
                            w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                        state = HCodeGroup.Alpha;
                            isAllUpper = true;
                        }
                    }
                    if (state == HCodeGroup.Delta && (cIn == ' ' || cIn == '.' || cIn == ','))
                    {
                        byte splCode = cIn == ',' ? (byte)0xC0 : (cIn == '.' ? (byte)0xE0 : (byte)0x00);
                        int splCodeLen = cIn == ',' ? 3 : (cIn == '.' ? 4 : 1);
                        w.AppendBits(Tables.UniStateSplCode, Tables.UniStateSplCodeLen);
                        w.AppendBits(splCode, splCodeLen);
                        l++;
                        continue;
                    }

                    int cCode = cIn - 32;
                    if (isAllUpper && isUpper)
                        cCode += 32;
                    if (cCode == 0)
                    {
                        if (state == HCodeGroup.Number)
                            w.AppendBits(Tables.UsxVCodes[Tables.NumSpcCode & 0x1F], Tables.UsxVCodeLens[Tables.NumSpcCode & 0x1F]);
                        else
                            w.AppendBits(Tables.UsxVCodes[1], Tables.UsxVCodeLens[1]);
                    }
                    else
                    {
                        cCode--;
                        w.AppendCode(Tables.UsxCode94[cCode], ref state, hCodes);
                    }
                    l++;
                }
                else if (cIn == 13 && cNext == 10)
                {
                    w.AppendCode(Tables.CrLfCode, ref state, hCodes);
                    l += 2;
                }
                else if (cIn == 10)
                {
                    if (state == HCodeGroup.Delta)
                    {
                        w.AppendBits(Tables.UniStateSplCode, Tables.UniStateSplCodeLen);
                        w.AppendBits(0xF0, 4);
                    }
                    else
                    {
                        w.AppendCode(Tables.LfCode, ref state, hCodes);
                    }
                    l++;
                }
                else if (cIn == 13)
                {
                    w.AppendCode(Tables.CrCode, ref state, hCodes);
                    l++;
                }
                else if (cIn == '\t')
                {
                    w.AppendCode(Tables.TabCode, ref state, hCodes);
                    l++;
                }
                else
                {
                    int uni = CoreHelpers.ReadUtf8(input, l, out int utf8Len);
                    if (uni != 0)
                    {
                        l += utf8Len;
                        if (state != HCodeGroup.Delta)
                        {
                            int uni2 = CoreHelpers.ReadUtf8(input, l, out _);
                            if (uni2 != 0)
                            {
                                if (state != HCodeGroup.Alpha)
                                {
                                    w.AppendSwitchCode(state);
                                    w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                                }
                                w.AppendSwitchCode(state);
                                w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                                w.AppendBits(Tables.UsxVCodes[1], Tables.UsxVCodeLens[1]);
                                state = HCodeGroup.Delta;
                            }
                            else
                            {
                                w.AppendSwitchCode(state);
                                w.AppendHCode(hCodes[HCodeGroup.Delta]);
                            }
                        }
                        w.EncodeUnicode(uni, prevUni);
                        prevUni = uni;
                    }
                    else
                    {
                        int binCount = 1;
                        for (int bi = l + 1; bi < len; bi++)
                        {
                            byte cBi = input[bi];
                            if (CoreHelpers.ReadUtf8(input, bi, out _) != 0)
                                break;
                            if (bi < len - 4 && cBi == input[bi - 1] && cBi == input[bi + 1] && cBi == input[bi + 2] && cBi == input[bi + 3])
                                break;
                            binCount++;
                        }
                        w.AppendNibbleEscape(state, hCodes);
                        w.AppendBits(0xF8, 5);
                        w.EncodeCount(binCount);
                        int remaining = binCount;
                        int p = l;
                        do
                        {
                            w.AppendBits(input[p], 8);
                            p++;
                        } while (--remaining != 0);
                        l = p;
                        continue;
                    }
                }
            }

            w.AppendFinalBits(state, isAllUpper, hCodes);
            return w.Buffer;
        }
    }
}
