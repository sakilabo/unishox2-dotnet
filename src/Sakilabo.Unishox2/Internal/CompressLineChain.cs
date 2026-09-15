using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// The compression side of ILineChain. ctx=0 is the whole source text of the element being compressed,
    /// and ctx&gt;=1 is the source text of an earlier element: the raw uncompressed bytes settled by the caller.
    /// </summary>
    internal sealed class CompressLineChain : ILineChain
    {
        private readonly byte[] _current;
        private readonly IReadOnlyList<byte[]> _previousElements;

        /// <param name="current">The whole source text of the element being compressed.</param>
        /// <param name="previousElements">The source text of the earlier elements, most recent first, so index 0 is the immediately preceding element.</param>
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
