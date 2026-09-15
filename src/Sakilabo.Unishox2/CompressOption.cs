using System;
using Sakilabo.Unishox2.Internal;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// テキストの圧縮・展開に使用する設定。定義済み設定で初期化し、各プロパティを変更できる。
    /// </summary>
    [Serializable]
    public sealed class CompressOption
    {
        private HCodes _hCodes;
        private string[] _frequentSequences;
        private string[] _templates;

        /// <summary>既定の設定で初期化する。</summary>
        public CompressOption()
        {
            _hCodes = Tables.HCodesDefault;
            _frequentSequences = (string[])Tables.FreqSeqDefault.Clone();
            _templates = Sakilabo.Unishox2.Templates.Default;
        }

        /// <summary>
        /// 水平符号5要素、頻出文字列最大6要素、テンプレート最大5要素で初期化する。
        /// 符号長は1～8、文字列要素はnull・空文字列を許容しない。
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

        /// <summary>水平符号(ALPHA/SYM/NUM/DICT/DELTA の5要素)。</summary>
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
        /// 頻出文字列(最大6要素、null・空文字列は不可)。
        /// </summary>
        public string[] FrequentSequences
        {
            get => _frequentSequences;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length > 6)
                    throw new ArgumentException("FrequentSequences は 6 要素以下である必要があります。", nameof(value));
                for (int i = 0; i < value.Length; i++)
                {
                    if (string.IsNullOrEmpty(value[i]))
                        throw new ArgumentException("FrequentSequences に null または空文字列は指定できません。", nameof(value));
                }
                _frequentSequences = value;
            }
        }

        /// <summary>
        /// 頻出パターンのテンプレート(最大5要素、null・空文字列は不可)。
        /// <see cref="Templates"/> の定数、または独自パターンを指定できる。
        /// </summary>
        public string[] Templates
        {
            get => _templates;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length > 5)
                    throw new ArgumentException("Templates は 5 要素以下である必要があります。", nameof(value));
                for (int i = 0; i < value.Length; i++)
                {
                    if (string.IsNullOrEmpty(value[i]))
                        throw new ArgumentException("Templates に null または空文字列は指定できません。", nameof(value));
                }
                _templates = value;
            }
        }

    }
}
