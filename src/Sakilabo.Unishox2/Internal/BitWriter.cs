using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// Writes a variable-length bit sequence, packed from the MSB, into a List&lt;byte&gt;.
    /// </summary>
    internal sealed class BitWriter
    {
        /// <summary>
        /// Internal exception used to abort a write that would exceed the configured output limit.
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

        /// <summary>The number of bits written so far.</summary>
        public int BitLength { get; private set; }

        /// <summary>The bytes written so far, with unused bits padded with zeros.</summary>
        public List<byte> Buffer => _buffer;

        public int ByteLength => (BitLength + 7) / 8;

        /// <summary>
        /// Puts subsequent AppendBits calls into a mode that throws WriteLimitExceededException as soon
        /// as a write would move past the given byte count into a new byte. Used to keep the terminator
        /// code from crossing the byte boundary that has already been settled.
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
        /// Writes the top len bits of code, taken from the MSB side.
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
        /// Pads the tail with the given value until the bit count is a multiple of 8.
        /// </summary>
        public void PadToByteBoundary(bool padWithOnes)
        {
            int rem = (8 - (BitLength % 8)) % 8;
            if (rem == 0)
                return;
            AppendBits(padWithOnes ? (byte)0xFF : (byte)0x00, rem);
        }

        /// <summary>Reads bit bitNo from what has been written, used to inspect the tail padding.</summary>
        public bool GetBit(int bitNo)
        {
            int byteIndex = bitNo / 8;
            if (byteIndex >= _buffer.Count)
                return false;
            return (_buffer[byteIndex] & (0x80 >> (bitNo % 8))) != 0;
        }

        /// <summary>
        /// Rewinds the write position back to an earlier bit. Subsequent writes are OR-ed onto
        /// the same byte positions.
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
