using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// 圧縮側の ILineChain。ctx=0 は現在圧縮中の要素の原文全体、ctx&gt;=1 は
    /// それより前の要素の原文(呼び出し元が確定させた、圧縮前の生バイト列)。
    /// </summary>
    internal sealed class CompressLineChain : ILineChain
    {
        private readonly byte[] _current;
        private readonly IReadOnlyList<byte[]> _previousElements;

        /// <param name="current">現在圧縮中の要素の原文全体。</param>
        /// <param name="previousElements">直前の要素から順(index 0 が直前の要素)に並べた、過去の要素の原文。</param>
        public CompressLineChain(byte[] current, IReadOnlyList<byte[]> previousElements)
        {
            _current = current;
            _previousElements = previousElements;
        }

        public int MaxCtx => _previousElements.Count;

        public int GetLength(int ctx)
        {
            return ctx == 0 ? _current.Length : _previousElements[ctx - 1].Length;
        }

        public byte GetByte(int ctx, int index)
        {
            return ctx == 0 ? _current[index] : _previousElements[ctx - 1][index];
        }
    }
}
