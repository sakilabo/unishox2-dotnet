namespace Sakilabo.Unishox2
{
    /// <summary>
    /// Named default templates.
    /// They can be assigned directly to <see cref="CompressOption.Templates"/>.
    /// </summary>
    public static class Templates
    {
        /// <summary>ISO date and time, for example 2026-09-14T12:34:56.789Z.</summary>
        public const string IsoDateTime = "tfff-of-tfTtf:rf:rf.fffZ";

        /// <summary>ISO date, for example 2026-09-14.</summary>
        public const string IsoDate = "tfff-of-tf";

        /// <summary>US phone number, for example (123) 456-7890.</summary>
        public const string UsPhoneNumber = "(fff) fff-ffff";

        /// <summary>ISO time, for example 12:34:56.</summary>
        public const string IsoTime = "tf:rf:rf";

        /// <summary>The default set of templates.</summary>
        public static string[] Default => new[] { IsoDateTime, IsoDate, UsPhoneNumber, IsoTime };
    }
}
