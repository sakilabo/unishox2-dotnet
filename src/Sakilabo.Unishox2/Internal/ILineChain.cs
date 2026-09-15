namespace Sakilabo.Unishox2.Internal
{
    internal interface ILineChain
    {
        /// <summary>有効な ctx の最大値。0 は「現在要素自身」のみ参照可能を意味する。</summary>
        int MaxCtx { get; }

        /// <summary>
        /// ctx 番目の要素で、参照検索(matchLine/decodeRepeat)から見える長さ。
        /// NUL バイトによる打ち切りは行わない(要素の実際の長さ)。
        /// </summary>
        int GetLength(int ctx);

        /// <summary>ctx 番目の要素の index 番目のバイト(0 &lt;= index &lt; GetLength(ctx))。</summary>
        byte GetByte(int ctx, int index);
    }
}
