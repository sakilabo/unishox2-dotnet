using System;

namespace Sakilabo.Unishox2
{
    /// <summary>
    /// Thrown when compression or decompression cannot continue because the input or output data is invalid.
    /// Examples include malformed UTF-8 byte sequences, strings containing unpaired surrogates,
    /// and corrupted compressed byte sequences.
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
