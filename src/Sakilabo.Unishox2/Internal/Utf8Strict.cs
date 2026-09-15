using System;
using System.Text;

namespace Sakilabo.Unishox2.Internal
{
    internal static class Utf8Strict
    {
        /// <summary>string を厳密な UTF-8 バイト列へ変換する。孤立サロゲートは例外にする。</summary>
        public static byte[] GetBytesStrict(string s)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                return encoding.GetBytes(s);
            }
            catch (EncoderFallbackException ex)
            {
                throw new UnishoxFormatException("入力文字列に孤立サロゲートなど、UTF-8 として表現できない文字が含まれています。", ex);
            }
        }

        /// <summary>UTF-8 バイト列を厳密に検証しながら string へ変換する。不正な符号列は例外にする。</summary>
        public static string GetStringStrict(byte[] bytes)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                return encoding.GetString(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw new UnishoxFormatException("入力バイト列が正しい UTF-8 として解釈できません。", ex);
            }
        }

        /// <summary>UTF-8 バイト列として妥当かどうかだけを検証する(文字列化しない)。</summary>
        public static void ValidateStrict(byte[] bytes)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                encoding.GetCharCount(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw new UnishoxFormatException("入力バイト列が正しい UTF-8 として解釈できません。", ex);
            }
        }
    }
}
