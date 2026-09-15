using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// Provides the predefined compression settings as <see cref="CompressOption"/> instances.
    /// </summary>
    public static class CompressOptions
    {
        /// <summary>Returns a new <see cref="CompressOption"/> with the default settings.</summary>
        public static CompressOption Default => new CompressOption();

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content made up of letters only.</summary>
        public static CompressOption AlphaOnly => new CompressOption(Tables.HCodesAlphaOnly, Tables.FreqSeqText);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content made up of letters and digits only.</summary>
        public static CompressOption AlphaNumOnly => new CompressOption(Tables.HCodesAlphaNumOnly, Tables.FreqSeqText);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content made up of letters, digits and symbols only.</summary>
        public static CompressOption AlphaNumSymOnly => new CompressOption(Tables.HCodesAlphaNumSymOnly, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> with the same settings as <see cref="AlphaNumSymOnly"/>, kept to mirror the upstream name USX_PSET_ALPHA_NUM_SYM_ONLY_TXT.</summary>
        public static CompressOption AlphaNumSymOnlyText => new CompressOption(Tables.HCodesAlphaNumSymOnly, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content dominated by letters.</summary>
        public static CompressOption FavorAlpha => new CompressOption(Tables.HCodesFavorAlpha, Tables.FreqSeqText);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content with many repetitions.</summary>
        public static CompressOption FavorDict => new CompressOption(Tables.HCodesFavorDict, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content dominated by symbols.</summary>
        public static CompressOption FavorSym => new CompressOption(Tables.HCodesFavorSym, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content with many umlauts and similar characters.</summary>
        public static CompressOption FavorUmlaut => new CompressOption(Tables.HCodesFavorUmlaut, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content without repetitions.</summary>
        public static CompressOption NoDict => new CompressOption(Tables.HCodesNoDict, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for content without Unicode characters.</summary>
        public static CompressOption NoUnicode => new CompressOption(Tables.HCodesNoUni, Tables.FreqSeqDefault);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for prose without Unicode characters.</summary>
        public static CompressOption NoUnicodeFavorText => new CompressOption(Tables.HCodesNoUni, Tables.FreqSeqText);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for URLs.</summary>
        public static CompressOption Url => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqUrl);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for JSON.</summary>
        public static CompressOption Json => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqJson);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for JSON without Unicode characters.</summary>
        public static CompressOption JsonNoUnicode => new CompressOption(Tables.HCodesNoUni, Tables.FreqSeqJson);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for XML.</summary>
        public static CompressOption Xml => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqXml);

        /// <summary>Returns a new <see cref="CompressOption"/> tuned for HTML.</summary>
        public static CompressOption Html => new CompressOption(Tables.HCodesDefault, Tables.FreqSeqHtml);
    }
}
