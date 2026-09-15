using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    internal sealed class DecompressLineChain : ILineChain
    {
        private readonly List<byte> _current;
        private readonly IReadOnlyList<byte[]> _previousElements;

        /// <param name="current">現在展開中の要素の出力バッファ(展開が進むにつれて伸びる)。</param>
        /// <param name="previousElements">直前の要素から順に並べた、過去の要素の展開済みバイト列。</param>
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
