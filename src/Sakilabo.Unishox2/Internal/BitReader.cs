namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// 圧縮バイト列を MSB からビット単位で読み出す。
    /// </summary>
    internal sealed class BitReader
    {
        private readonly byte[] _input;

        public BitReader(byte[] input)
        {
            _input = input;
            LengthBits = input.Length * 8;
        }

        /// <summary>入力全体のビット長。</summary>
        public int LengthBits { get; }

        public bool ReadBit(int bitNo)
        {
            return (_input[bitNo >> 3] & (0x80 >> (bitNo % 8))) != 0;
        }

        /// <summary>
        /// bitNo から 8 ビット分を読む(バイト境界を跨いでもよい)。末尾を越える分は 1 で埋める。
        /// </summary>
        public byte Read8BitCode(int bitNo)
        {
            int bitPos = bitNo & 0x07;
            int charPos = bitNo >> 3;
            int lenBytes = LengthBits >> 3;
            byte code = (byte)(_input[charPos] << bitPos);
            charPos++;
            if (charPos < lenBytes)
            {
                code |= (byte)(_input[charPos] >> (8 - bitPos));
            }
            else
            {
                code |= (byte)(0xFF >> (8 - bitPos));
            }
            return code;
        }

        /// <summary>連続する 1 ビットの個数を limit まで数え、終端の 0 を読み飛ばす。</summary>
        public int? GetStepCodeIdx(ref int bitNo, int limit)
        {
            int idx = 0;
            while (bitNo < LengthBits && ReadBit(bitNo))
            {
                idx++;
                bitNo++;
                if (idx == limit)
                    return idx;
            }
            if (bitNo >= LengthBits)
                return null;
            bitNo++;
            return idx;
        }

        /// <summary>bitNo から count ビットを読み取る。読み取り位置は進めない。</summary>
        public long GetNumFromBits(int bitNo, int count)
        {
            long ret = 0;
            while (count-- > 0 && bitNo < LengthBits)
            {
                if (ReadBit(bitNo))
                    ret += 1L << count;
                bitNo++;
            }
            return count < 0 ? ret : -1;
        }

        /// <summary>可変長のカウント値を読み取る。</summary>
        public long ReadCount(ref int bitNo)
        {
            int? index = GetStepCodeIdx(ref bitNo, 4);
            if (!index.HasValue)
                return -1;
            int idx = index.Value;
            if (bitNo + Tables.CountBitLens[idx] - 1 >= LengthBits)
                return -1;
            long count = GetNumFromBits(bitNo, Tables.CountBitLens[idx]) + (idx != 0 ? Tables.CountAdder[idx - 1] : 0);
            bitNo += Tables.CountBitLens[idx];
            return count;
        }

        /// <summary>
        /// 特殊コード(スペース/カンマ/ピリオド/改行/切替/終端)は
        /// (IsSpecial:true, SpecialCode) で返し、通常の Unicode 差分は (false, delta) で返す。
        /// </summary>
        public (bool IsSpecial, int SpecialCode, long Delta)? ReadUnicode(ref int bitNo)
        {
            int? index = GetStepCodeIdx(ref bitNo, 5);
            if (!index.HasValue)
                return null;
            int idx = index.Value;
            if (idx == 5)
            {
                index = GetStepCodeIdx(ref bitNo, 4);
                return index.HasValue ? (true, index.Value, 0) : ((bool, int, long)?)null;
            }
            int sign = bitNo < LengthBits && ReadBit(bitNo) ? 1 : 0;
            bitNo++;
            if (bitNo + Tables.UniBitLen[idx] - 1 >= LengthBits)
                return null;
            long count = GetNumFromBits(bitNo, Tables.UniBitLen[idx]);
            count += Tables.UniAdder[idx];
            bitNo += Tables.UniBitLen[idx];
            return (false, 0, sign != 0 ? -count : count);
        }

        /// <summary>垂直符号を読み取り、符号表のインデックスを返す。</summary>
        public int? ReadVCodeIdx(ref int bitNo)
        {
            if (bitNo < LengthBits)
            {
                byte code = Read8BitCode(bitNo);
                for (int i = 0; i < Tables.SectionCount; i++)
                {
                    if (code <= Tables.UsxVSections[i])
                    {
                        byte vcode = Tables.UsxVCodeLookup[Tables.UsxVSectionPos[i] + ((code & Tables.UsxVSectionMask[i]) >> Tables.UsxVSectionShift[i])];
                        bitNo += (vcode >> 5) + 1;
                        if (bitNo > LengthBits)
                            return null;
                        return vcode & 0x1F;
                    }
                }
            }
            return null;
        }

        /// <summary>水平符号を読み取り、文字グループのインデックスを返す。</summary>
        public HCodeGroup? ReadHCodeIdx(ref int bitNo, HCodes hCodes)
        {
            if (!hCodes[HCodeGroup.Alpha].HasValue)
                return HCodeGroup.Alpha;
            if (bitNo < LengthBits)
            {
                byte code = Read8BitCode(bitNo);
                for (int codePos = 0; codePos < 5; codePos++)
                {
                    (byte Code, byte Length)? hCode = hCodes[codePos];
                    if (hCode.HasValue && (code & Tables.LenMasks[hCode.Value.Length - 1]) == hCode.Value.Code)
                    {
                        bitNo += hCode.Value.Length;
                        return (HCodeGroup)codePos;
                    }
                }
            }
            return null;
        }
    }
}
