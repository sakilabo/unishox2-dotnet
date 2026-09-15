using System;
using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// Settings used to compress and decompress text. Initialize from a predefined set of settings
    /// and adjust individual properties as needed.
    /// </summary>
    [Serializable]
    public sealed class CompressOption
    {
        private HCodes _hCodes;
        private string[] _frequentSequences;
        private string[] _templates;

        /// <summary>Initializes an instance with the default settings.</summary>
        public CompressOption()
        {
            _hCodes = Tables.HCodesDefault;
            _frequentSequences = (string[])Tables.FreqSeqDefault.Clone();
            _templates = Sakilabo.Unishox2.Templates.Default;
        }

        /// <summary>
        /// Initializes an instance with five horizontal codes, at most six frequent sequences and
        /// at most five templates. Code lengths run from 1 to 8, and string elements may be neither
        /// null nor empty.
        /// </summary>
        internal CompressOption(HCodes hCodes, string[] frequentSequences, string[]? templates = null)
            : this()
        {
            HCodes = hCodes;
            FrequentSequences = (string[])frequentSequences.Clone();
            Templates = templates == null
                ? Sakilabo.Unishox2.Templates.Default
                : (string[])templates.Clone();
        }

        /// <summary>Horizontal codes: the five entries for ALPHA, SYM, NUM, DICT and DELTA.</summary>
        public HCodes HCodes
        {
            get => _hCodes;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _hCodes = value;
            }
        }

        /// <summary>
        /// Frequent sequences: at most six elements, neither null nor empty.
        /// </summary>
        public string[] FrequentSequences
        {
            get => _frequentSequences;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length > 6)
                    throw new ArgumentException("FrequentSequences must have 6 or fewer elements.", nameof(value));
                for (int i = 0; i < value.Length; i++)
                {
                    if (string.IsNullOrEmpty(value[i]))
                        throw new ArgumentException("FrequentSequences must not contain null or empty elements.", nameof(value));
                }
                _frequentSequences = value;
            }
        }

        /// <summary>
        /// Templates for recurring patterns: at most five elements, neither null nor empty.
        /// The constants on <see cref="Templates"/> or custom patterns may be used.
        /// </summary>
        public string[] Templates
        {
            get => _templates;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length > 5)
                    throw new ArgumentException("Templates must have 5 or fewer elements.", nameof(value));
                for (int i = 0; i < value.Length; i++)
                {
                    if (string.IsNullOrEmpty(value[i]))
                        throw new ArgumentException("Templates must not contain null or empty elements.", nameof(value));
                }
                _templates = value;
            }
        }

    }
}
