using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// 定義済みの圧縮設定を <see cref="CompressOption"/> として提供する。
    /// </summary>
    public static class CompressOptions
    {
        /// <summary>既定の設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption Default => new CompressOption();

        /// <summary>英字のみの内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption AlphaOnly => new CompressOption(Tables.HCodesAlphaOnly, Tables.FreqSeqText);

        /// <summary>英数字のみの内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption AlphaNumOnly => new CompressOption(Tables.HCodesAlphaNumOnly, Tables.FreqSeqText);

        /// <summary>英数字・記号のみの内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption AlphaNumSymOnly => new CompressOption(Tables.HCodesAlphaNumSymOnly, Tables.FreqSeqDefault);

        /// <summary>英数字・記号中心で文章の多い内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption AlphaNumSymOnlyText => new CompressOption(Tables.HCodesAlphaNumSymOnly, Tables.FreqSeqDefault);

        /// <summary>英字が多い内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption FavorAlpha => new CompressOption(Tables.HCodesFavorAlpha, Tables.FreqSeqText);

        /// <summary>繰り返しが多い内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption FavorDict => new CompressOption(Tables.HCodesFavorDict, Tables.FreqSeqDefault);

        /// <summary>記号が多い内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption FavorSym => new CompressOption(Tables.HCodesFavorSym, Tables.FreqSeqDefault);

        /// <summary>ウムラウト等の文字が多い内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption FavorUmlaut => new CompressOption(Tables.HCodesFavorUmlaut, Tables.FreqSeqDefault);

        /// <summary>繰り返しがない内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption NoDict => new CompressOption(Tables.HCodesNoDict, Tables.FreqSeqDefault);

        /// <summary>Unicode文字を含まない内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption NoUnicode => new CompressOption(Tables.HCodesNoUni, Tables.FreqSeqDefault);

        /// <summary>Unicode文字を含まず文章中心の内容向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption NoUnicodeFavorText => new CompressOption(Tables.HCodesNoUni, Tables.FreqSeqText);

        /// <summary>URL向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption Url => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqUrl);

        /// <summary>JSON向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption Json => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqJson);

        /// <summary>Unicode文字を含まないJSON向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption JsonNoUnicode => new CompressOption(Tables.HCodesNoUni, Tables.FreqSeqJson);

        /// <summary>XML向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption Xml => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqXml);

        /// <summary>HTML向けの設定による新しい <see cref="CompressOption"/> を返す。</summary>
        public static CompressOption Html => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqHtml);
    }
}
