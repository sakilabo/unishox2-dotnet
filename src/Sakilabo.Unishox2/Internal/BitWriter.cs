using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// MSB から詰める可変長ビット列を List&lt;byte&gt; へ書き込む。
    /// </summary>
    internal sealed class BitWriter
    {
        /// <summary>
        /// 指定された出力上限を超える書き込みを中断するための内部例外。
        /// </summary>
        internal sealed class WriteLimitExceededException : System.Exception
        {
        }

        private readonly List<byte> _buffer;
        private int? _limitBytes;

        public BitWriter()
        {
            _buffer = new List<byte>();
        }

        /// <summary>現在までに書き込んだビット数。</summary>
        public int BitLength { get; private set; }

        /// <summary>これまでに書き込んだバイト列(未使用ビットは 0 埋め)。</summary>
        public List<byte> Buffer => _buffer;

        public int ByteLength => (BitLength + 7) / 8;

        /// <summary>
        /// 以降の AppendBits を、指定バイト数を超えて新しいバイトへ書き込もうとした時点で
        /// WriteLimitExceededException を送出するモードにする。終端コードが確定済みの
        /// バイト境界を超えないようにするために使う。
        /// </summary>
        public void SetByteLimit(int limitBytes)
        {
            _limitBytes = limitBytes;
        }

        public void ClearByteLimit()
        {
            _limitBytes = null;
        }

        /// <summary>
        /// code の上位 len ビット(MSB 側)を書き込む。
        /// </summary>
        public void AppendBits(byte code, int len)
        {
            while (len > 0)
            {
                int curBit = BitLength % 8;
                int blen = len;
                byte aByte = (byte)(code & Tables.UsxMask[blen - 1]);
                aByte = (byte)(aByte >> curBit);
                if (blen + curBit > 8)
                    blen = 8 - curBit;
                int byteIndex = BitLength / 8;
                if (_limitBytes.HasValue && byteIndex >= _limitBytes.Value)
                    throw new WriteLimitExceededException();
                while (_buffer.Count <= byteIndex)
                    _buffer.Add(0);
                if (curBit == 0)
                    _buffer[byteIndex] = aByte;
                else
                    _buffer[byteIndex] = (byte)(_buffer[byteIndex] | aByte);
                code = (byte)(code << blen);
                BitLength += blen;
                len -= blen;
            }
        }

        /// <summary>
        /// 末尾を 8 の倍数ビットまで指定値で埋める。
        /// </summary>
        public void PadToByteBoundary(bool padWithOnes)
        {
            int rem = (8 - (BitLength % 8)) % 8;
            if (rem == 0)
                return;
            AppendBits(padWithOnes ? (byte)0xFF : (byte)0x00, rem);
        }

        /// <summary>書き込み済みの bitNo 番目のビットを読む(末尾パディング判定用)。</summary>
        public bool GetBit(int bitNo)
        {
            int byteIndex = bitNo / 8;
            if (byteIndex >= _buffer.Count)
                return false;
            return (_buffer[byteIndex] & (0x80 >> (bitNo % 8))) != 0;
        }

        /// <summary>
        /// 書き込み位置を過去のビット位置まで巻き戻す。以降の書き込みは同じバイト位置へ
        /// OR で重ねられる。
        /// </summary>
        public void Rewind(int bitLength)
        {
            BitLength = bitLength;
        }

        public byte[] ToArray()
        {
            var result = new byte[ByteLength];
            for (int i = 0; i < result.Length; i++)
                result[i] = _buffer[i];
            return result;
        }
    }
}
