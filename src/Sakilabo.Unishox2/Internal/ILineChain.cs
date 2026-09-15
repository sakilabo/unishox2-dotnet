namespace Sakilabo.Unishox2.Internal
{
    internal interface ILineChain
    {
        /// <summary>The largest valid ctx. 0 means only the current element itself can be referenced.</summary>
        int MaxCtx { get; }

        /// <summary>
        /// The length of element ctx as seen by the reference search (matchLine and decodeRepeat).
        /// NUL bytes do not truncate it; this is the actual length of the element.
        /// </summary>
        int GetLength(int ctx);

        /// <summary>Byte index of element ctx, where 0 &lt;= index &lt; GetLength(ctx).</summary>
        byte GetByte(int ctx, int index);
    }
}
