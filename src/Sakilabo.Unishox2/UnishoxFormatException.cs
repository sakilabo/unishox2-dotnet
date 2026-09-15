using System;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// 圧縮・展開の入出力データが不正なために処理を継続できない場合に送出される例外。
    /// 例: 不正な UTF-8 バイト列、孤立サロゲートを含む文字列、壊れた圧縮バイト列など。
    /// </summary>
    public sealed class UnishoxFormatException : Exception
    {
        public UnishoxFormatException(string message) : base(message)
        {
        }

        public UnishoxFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
