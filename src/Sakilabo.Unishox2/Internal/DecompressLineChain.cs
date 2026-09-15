using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal sealed class DecompressLineChain : ILineChain
    {
        private readonly List<byte> _current;
        private readonly IReadOnlyList<byte[]> _previousElements;

        /// <param name="current">The output buffer of the element being decompressed; it grows as decompression proceeds.</param>
        /// <param name="previousElements">The decompressed byte sequences of the earlier elements, most recent first.</param>
        public DecompressLineChain(List<byte> current, IReadOnlyList<byte[]> previousElements)
        {
            _current = current;
            _previousElements = previousElements;
        }

        public int MaxCtx => _previousElements.Count;

        public int GetLength(int ctx)
        {
            return ctx == 0 ? _current.Count : _previousElements[ctx - 1].Length;
        }

        public byte GetByte(int ctx, int index)
        {
            return ctx == 0 ? _current[index] : _previousElements[ctx - 1][index];
        }
    }
}
