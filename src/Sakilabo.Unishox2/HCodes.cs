using System;
using System.Collections;
using System.Collections.Generic;

namespace Sakilabo.Unishox2
{
    public enum HCodeGroup
    {
        Alpha,
        Symbol,
        Number,
        Dictionary,
        Delta,
    }

    /// <summary>ALPHA、SYM、NUM、DICT、DELTA の水平符号。使用しないグループは null で指定する。</summary>
    [Serializable]
    public sealed class HCodes : IReadOnlyList<(byte Code, byte Length)?>
    {
        private readonly (byte Code, byte Length)?[] _values;

        public HCodes()
            : this(null, null, null, null, null)
        {
        }

        public HCodes(
            (byte Code, byte Length)? alpha,
            (byte Code, byte Length)? symbol,
            (byte Code, byte Length)? number,
            (byte Code, byte Length)? dictionary,
            (byte Code, byte Length)? delta)
        {
            _values = new (byte Code, byte Length)?[] { alpha, symbol, number, dictionary, delta };
            for (int i = 0; i < _values.Length; i++)
            {
                (byte Code, byte Length)? value = _values[i];
                if (value.HasValue && (value.Value.Length == 0 || value.Value.Length > 8))
                    throw new ArgumentOutOfRangeException("Length", "符号長は1～8の範囲で指定します。");
            }
        }

        public int Count => 5;
        public (byte Code, byte Length)? this[HCodeGroup group] => _values[(int)group];
        public (byte Code, byte Length)? this[int index] => _values[index];
        public IEnumerator<(byte Code, byte Length)?> GetEnumerator() => ((IEnumerable<(byte Code, byte Length)?>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
