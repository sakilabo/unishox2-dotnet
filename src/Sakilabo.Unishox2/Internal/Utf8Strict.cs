using System;
using System.Text;

namespace Sakilabo.Unishox2.Internal
{
    internal static class Utf8Strict
    {
        /// <summary>Converts a string to a strict UTF-8 byte sequence. Unpaired surrogates raise an exception.</summary>
        public static byte[] GetBytesStrict(string s)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                return encoding.GetBytes(s);
            }
            catch (EncoderFallbackException ex)
            {
                throw new UnishoxFormatException("The input string contains characters that cannot be represented in UTF-8, such as unpaired surrogates.", ex);
            }
        }

        /// <summary>Converts a UTF-8 byte sequence to a string with strict validation. Invalid sequences raise an exception.</summary>
        public static string GetStringStrict(byte[] bytes)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                return encoding.GetString(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw new UnishoxFormatException("The input byte sequence is not valid UTF-8.", ex);
            }
        }

        /// <summary>Validates only that the byte sequence is well-formed UTF-8, without producing a string.</summary>
        public static void ValidateStrict(byte[] bytes)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                encoding.GetCharCount(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw new UnishoxFormatException("The input byte sequence is not valid UTF-8.", ex);
            }
        }
    }
}
