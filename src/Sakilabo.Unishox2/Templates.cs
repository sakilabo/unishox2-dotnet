namespace Sakilabo.Unishox2
{
    /// <summary>
    /// 名前付きの既定テンプレート集合。
    /// <see cref="CompressOption.Templates"/> にそのまま指定できる。
    /// </summary>
    public static class Templates
    {
        /// <summary>ISO 日時 (例: 2026-09-14T12:34:56.789Z)。</summary>
        public const string IsoDateTime = "tfff-of-tfTtf:rf:rf.fffZ";

        /// <summary>ISO 日付 (例: 2026-09-14)。</summary>
        public const string IsoDate = "tfff-of-tf";

        /// <summary>米国電話番号 (例: (123) 456-7890)。</summary>
        public const string UsPhoneNumber = "(fff) fff-ffff";

        /// <summary>ISO 時刻 (例: 12:34:56)。</summary>
        public const string IsoTime = "tf:rf:rf";

        /// <summary>既定テンプレート集合。</summary>
        public static string[] Default => new[] { IsoDateTime, IsoDate, UsPhoneNumber, IsoTime };
    }
}
