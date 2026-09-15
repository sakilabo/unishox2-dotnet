using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal enum NibbleType
    {
        Num = 0,
        HexLower = 1,
        HexUpper = 2,
        Not = 3,
    }

    internal static class CoreHelpers
    {
        public static void AppendSwitchCode(this BitWriter w, HCodeGroup state)
        {
            if (state == HCodeGroup.Delta)
            {
                w.AppendBits(Tables.UniStateSplCode, Tables.UniStateSplCodeLen);
                w.AppendBits(Tables.UniStateSwCode, Tables.UniStateSwCodeLen);
            }
            else
            {
                w.AppendBits(Tables.SwCode, Tables.SwCodeLen);
            }
        }

        private static readonly string[] GroupNames = { "ALPHA", "SYM", "NUM", "DICT", "DELTA" };

        /// <summary>
        /// Writes the horizontal and vertical codes for a character group and updates state.
        /// A group without a horizontal code cannot be represented, so UnishoxFormatException is thrown.
        /// </summary>
        public static void AppendCode(this BitWriter w, int code, ref HCodeGroup state, HCodes hCodes)
        {
            HCodeGroup hcode = (HCodeGroup)(code >> 5);
            int vcode = code & 0x1F;
            if (!hCodes[hcode].HasValue && hcode != HCodeGroup.Alpha)
            {
                throw new UnishoxFormatException(
                    $"The current CompressOption (a predefined set from CompressOptions, or a customised one) has no code for character group '{GroupNames[(int)hcode]}', " +
                    "so the matching characters in the input cannot be compressed. " +
                    "Review the predefined settings in CompressOptions, or the HCodes value.");
            }
            switch (hcode)
            {
                case HCodeGroup.Alpha:
                    if (state != HCodeGroup.Alpha)
                    {
                        w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Alpha]);
                        state = HCodeGroup.Alpha;
                    }
                    break;
                case HCodeGroup.Symbol:
                    w.AppendSwitchCode(state);
                    w.AppendHCode(hCodes[HCodeGroup.Symbol]);
                    break;
                case HCodeGroup.Number:
                    if (state != HCodeGroup.Number)
                    {
                        w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Number]);
                        byte ch = Tables.UsxSets[(int)hcode][vcode];
                        if (ch >= '0' && ch <= '9')
                            state = HCodeGroup.Number;
                    }
                    break;
            }
            w.AppendBits(Tables.UsxVCodes[vcode], Tables.UsxVCodeLens[vcode]);
        }

        /// <summary>Writes an integer as a variable-length count code.</summary>
        public static void EncodeCount(this BitWriter w, int count)
        {
            for (int i = 0; i < 5; i++)
            {
                if (count < Tables.CountAdder[i])
                {
                    w.AppendBits((byte)(Tables.CountCodes[i] & 0xF8), Tables.CountCodes[i] & 0x07);
                    int count16 = (count - (i != 0 ? Tables.CountAdder[i - 1] : 0)) << (16 - Tables.CountBitLens[i]);
                    if (Tables.CountBitLens[i] > 8)
                    {
                        w.AppendBits((byte)(count16 >> 8), 8);
                        w.AppendBits((byte)(count16 & 0xFF), Tables.CountBitLens[i] - 8);
                    }
                    else
                    {
                        w.AppendBits((byte)(count16 >> 8), Tables.CountBitLens[i]);
                    }
                    return;
                }
            }
        }

        /// <summary>Encodes the delta between Unicode code points.</summary>
        public static void EncodeUnicode(this BitWriter w, int code, int prevCode)
        {
            long till = 0;
            long diff = code - prevCode;
            if (diff < 0)
                diff = -diff;
            for (int i = 0; i < 5; i++)
            {
                till += 1L << Tables.UniBitLen[i];
                if (diff < till)
                {
                    w.AppendBits((byte)(Tables.UnicodeCodes[i] & 0xF8), Tables.UnicodeCodes[i] & 0x07);
                    w.AppendBits((byte)(prevCode > code ? 0x80 : 0), 1);
                    long val = diff - Tables.UniAdder[i];
                    int bitLen = Tables.UniBitLen[i];
                    if (bitLen > 16)
                    {
                        val <<= (24 - bitLen);
                        w.AppendBits((byte)((val >> 16) & 0xFF), 8);
                        w.AppendBits((byte)((val >> 8) & 0xFF), 8);
                        w.AppendBits((byte)(val & 0xFF), bitLen - 16);
                    }
                    else if (bitLen > 8)
                    {
                        val <<= (16 - bitLen);
                        w.AppendBits((byte)((val >> 8) & 0xFF), 8);
                        w.AppendBits((byte)(val & 0xFF), bitLen - 8);
                    }
                    else
                    {
                        val <<= (8 - bitLen);
                        w.AppendBits((byte)(val & 0xFF), bitLen);
                    }
                    return;
                }
            }
        }

        /// <summary>Writes the escape code that marks the start of a nibble sequence.</summary>
        public static void AppendNibbleEscape(this BitWriter w, HCodeGroup state, HCodes hCodes)
        {
            w.AppendSwitchCode(state);
            w.AppendHCode(hCodes[HCodeGroup.Number]);
            w.AppendBits(0, 2);
        }

        public static void AppendHCode(this BitWriter w, (byte Code, byte Length)? hCode)
        {
            (byte Code, byte Length) value = hCode.GetValueOrDefault();
            w.AppendBits(value.Code, value.Length);
        }

        /// <summary>
        /// Writes the terminator code into the free bits of the final byte. When the terminator would
        /// spill into the next byte it is not written, and the decoder treats the end of the input as the terminator.
        /// </summary>
        public static void AppendFinalBits(this BitWriter w, HCodeGroup state, bool isAllUpper, HCodes hCodes)
        {
            int limitBytes = (w.BitLength + 7) / 8;
            w.SetByteLimit(limitBytes);
            try
            {
                if (hCodes[HCodeGroup.Alpha].HasValue)
                {
                    if (HCodeGroup.Number != state)
                    {
                        w.AppendSwitchCode(state);
                        w.AppendHCode(hCodes[HCodeGroup.Number]);
                    }
                    w.AppendBits(Tables.UsxVCodes[Tables.TermCode & 0x1F], Tables.UsxVCodeLens[Tables.TermCode & 0x1F]);
                }
                else
                {
                    w.AppendBits(Tables.TermByteAlphaOnly, isAllUpper ? Tables.TermByteAlphaOnlyLenUpper : Tables.TermByteAlphaOnlyLenLower);
                }
            }
            catch (BitWriter.WriteLimitExceededException)
            {
                // Skip a terminator code that would cross the byte boundary.
            }
            finally
            {
                w.ClearByteLimit();
            }

            bool padWithOnes = w.BitLength != 0 && w.GetBit(w.BitLength - 1);
            w.PadToByteBoundary(padWithOnes);
        }

        public static byte GetBaseCode(char ch)
        {
            if (ch >= '0' && ch <= '9')
                return (byte)((ch - '0') << 4);
            if (ch >= 'A' && ch <= 'F')
                return (byte)((ch - 'A' + 10) << 4);
            if (ch >= 'a' && ch <= 'f')
                return (byte)((ch - 'a' + 10) << 4);
            return 0;
        }

        public static NibbleType GetNibbleType(char ch)
        {
            if (ch >= '0' && ch <= '9')
                return NibbleType.Num;
            if (ch >= 'a' && ch <= 'f')
                return NibbleType.HexLower;
            if (ch >= 'A' && ch <= 'F')
                return NibbleType.HexUpper;
            return NibbleType.Not;
        }

        public static char GetHexChar(int nibble, NibbleType hexType)
        {
            if (nibble >= 0 && nibble <= 9)
                return (char)('0' + nibble);
            if (hexType < NibbleType.HexUpper)
                return (char)('a' + nibble - 10);
            return (char)('A' + nibble - 10);
        }

        /// <summary>
        /// Reads a multi-byte UTF-8 character from input at the given index.
        /// Returns 0 for a single byte (ASCII) or an undecodable sequence, which the caller branches on.
        /// </summary>
        public static int ReadUtf8(byte[] input, int l, out int utf8Len)
        {
            int len = input.Length;
            utf8Len = 0;
            if (l < len - 1 && (input[l] & 0xE0) == 0xC0 && (input[l + 1] & 0xC0) == 0x80)
            {
                utf8Len = 2;
                int ret = (input[l] & 0x1F) << 6;
                ret += input[l + 1] & 0x3F;
                return ret < 0x80 ? 0 : ret;
            }
            if (l < len - 2 && (input[l] & 0xF0) == 0xE0 && (input[l + 1] & 0xC0) == 0x80 && (input[l + 2] & 0xC0) == 0x80)
            {
                utf8Len = 3;
                int ret = (input[l] & 0x0F) << 6;
                ret += input[l + 1] & 0x3F;
                ret <<= 6;
                ret += input[l + 2] & 0x3F;
                return ret < 0x0800 ? 0 : ret;
            }
            if (l < len - 3 && (input[l] & 0xF8) == 0xF0 && (input[l + 1] & 0xC0) == 0x80 && (input[l + 2] & 0xC0) == 0x80 && (input[l + 3] & 0xC0) == 0x80)
            {
                utf8Len = 4;
                int ret = (input[l] & 0x07) << 6;
                ret += input[l + 1] & 0x3F;
                ret <<= 6;
                ret += input[l + 2] & 0x3F;
                ret <<= 6;
                ret += input[l + 3] & 0x3F;
                return ret < 0x10000 ? 0 : ret;
            }
            return 0;
        }

        /// <summary>Writes a Unicode code point to the output as a UTF-8 byte sequence.</summary>
        public static void WriteUtf8(List<byte> output, int uni)
        {
            if (uni < (1 << 11))
            {
                output.Add((byte)(0xC0 + (uni >> 6)));
                output.Add((byte)(0x80 + (uni & 0x3F)));
            }
            else if (uni < (1 << 16))
            {
                output.Add((byte)(0xE0 + (uni >> 12)));
                output.Add((byte)(0x80 + ((uni >> 6) & 0x3F)));
                output.Add((byte)(0x80 + (uni & 0x3F)));
            }
            else
            {
                output.Add((byte)(0xF0 + (uni >> 18)));
                output.Add((byte)(0x80 + ((uni >> 12) & 0x3F)));
                output.Add((byte)(0x80 + ((uni >> 6) & 0x3F)));
                output.Add((byte)(0x80 + (uni & 0x3F)));
            }
        }
    }
}
