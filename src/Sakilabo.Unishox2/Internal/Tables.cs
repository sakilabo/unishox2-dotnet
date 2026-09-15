using System.Collections.Generic;

namespace Sakilabo.Unishox2.Internal
{
    /// <summary>
    /// Code tables, character sets and predefined setting values shared by compression and decompression.
    /// </summary>
    internal static class Tables
    {
        public const int NiceLen = 5;

        // Set (USX_NUM - 2) and vertical code (26) for encoding repeating letters
        public const int RptCode = ((int)HCodeGroup.Number << 5) + 26;
        // Set (USX_NUM - 2) and vertical code (27) for encoding terminator
        public const int TermCode = ((int)HCodeGroup.Number << 5) + 27;
        // Set (USX_SYM - 1) and vertical code (7) for encoding Line feed \n
        public const int LfCode = ((int)HCodeGroup.Symbol << 5) + 7;
        // Set (USX_NUM - 1) and vertical code (8) for encoding \r\n
        public const int CrLfCode = ((int)HCodeGroup.Symbol << 5) + 8;
        // Set (USX_NUM - 1) and vertical code (22) for encoding \r
        public const int CrCode = ((int)HCodeGroup.Symbol << 5) + 22;
        // Set (USX_NUM - 1) and vertical code (14) for encoding \t
        public const int TabCode = ((int)HCodeGroup.Symbol << 5) + 14;
        // Set (USX_NUM - 2) and vertical code (17) for space character when it appears in USX_NUM state
        public const int NumSpcCode = ((int)HCodeGroup.Number << 5) + 17;

        public const byte UniStateSplCode = 0xF8;
        public const int UniStateSplCodeLen = 5;
        public const byte UniStateSwCode = 0x80;
        public const int UniStateSwCodeLen = 2;

        public const byte SwCode = 0;
        public const int SwCodeLen = 2;
        public const byte TermByteAlphaOnly = 0;
        public const int TermByteAlphaOnlyLenLower = 6;
        public const int TermByteAlphaOnlyLenUpper = 4;

        public const int UsxOffset94 = 33;

        // usx_sets: the 3x28 character set table for ALPHA, SYM and NUM. 0 means "no character".
        public static readonly byte[][] UsxSets = new byte[][]
        {
            new byte[] {  0, (byte)' ', (byte)'e', (byte)'t', (byte)'a', (byte)'o', (byte)'i', (byte)'n',
                        (byte)'s', (byte)'r', (byte)'l', (byte)'c', (byte)'d', (byte)'h', (byte)'u', (byte)'p', (byte)'m', (byte)'b',
                        (byte)'g', (byte)'w', (byte)'f', (byte)'y', (byte)'v', (byte)'k', (byte)'q', (byte)'j', (byte)'x', (byte)'z' },
            new byte[] { (byte)'"', (byte)'{', (byte)'}', (byte)'_', (byte)'<', (byte)'>', (byte)':', (byte)'\n',
                        0, (byte)'[', (byte)']', (byte)'\\', (byte)';', (byte)'\'', (byte)'\t', (byte)'@', (byte)'*', (byte)'&',
                        (byte)'?', (byte)'!', (byte)'^', (byte)'|', (byte)'\r', (byte)'~', (byte)'`', 0, 0, 0 },
            new byte[] {  0, (byte)',', (byte)'.', (byte)'0', (byte)'1', (byte)'9', (byte)'2', (byte)'5', (byte)'-',
                        (byte)'/', (byte)'3', (byte)'4', (byte)'6', (byte)'7', (byte)'8', (byte)'(', (byte)')', (byte)' ',
                        (byte)'=', (byte)'+', (byte)'$', (byte)'%', (byte)'#', 0, 0, 0, 0, 0 },
        };

        // usx_vcodes: vertical codes packed from the MSB
        public static readonly byte[] UsxVCodes = new byte[]
        {
            0x00, 0x40, 0x60, 0x80, 0x90, 0xA0, 0xB0,
            0xC0, 0xD0, 0xD8, 0xE0, 0xE4, 0xE8, 0xEC,
            0xEE, 0xF0, 0xF2, 0xF4, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
        };

        public static readonly byte[] UsxVCodeLens = new byte[]
        {
            2, 3, 3, 4, 4, 4, 4,
            4, 5, 5, 6, 6, 6, 7,
            7, 7, 7, 7, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8,
        };

        // Vertical Codes and Set number for frequent sequences in sets USX_SYM and USX_NUM.
        public static readonly byte[] UsxFreqCodes = new byte[]
        {
            (byte)((1 << 5) + 25), (byte)((1 << 5) + 26), (byte)((1 << 5) + 27),
            (byte)((2 << 5) + 23), (byte)((2 << 5) + 24), (byte)((2 << 5) + 25),
        };

        public static readonly byte[] UsxMask = new byte[] { 0x80, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC, 0xFE, 0xFF };

        public static readonly byte[] CountBitLens = new byte[] { 2, 4, 7, 11, 16 };
        public static readonly int[] CountAdder = new int[] { 4, 20, 148, 2196, 67732 };
        public static readonly byte[] CountCodes = new byte[] { 0x01, 0x82, 0xC3, 0xE4, 0xF4 };

        public static readonly byte[] UniBitLen = new byte[] { 6, 12, 14, 16, 21 };
        public static readonly int[] UniAdder = new int[] { 0, 64, 4160, 20544, 86080 };
        public static readonly byte[] UnicodeCodes = new byte[] { 0x01, 0x82, 0xC3, 0xE4, 0xF5, 0xFD };

        public const int SectionCount = 5;
        public static readonly byte[] UsxVSections = new byte[] { 0x7F, 0xBF, 0xDF, 0xEF, 0xFF };
        public static readonly byte[] UsxVSectionPos = new byte[] { 0, 4, 8, 12, 20 };
        public static readonly byte[] UsxVSectionMask = new byte[] { 0x7F, 0x3F, 0x1F, 0x0F, 0x0F };
        public static readonly byte[] UsxVSectionShift = new byte[] { 5, 4, 3, 1, 0 };

        // code len is one less as 8 cannot be accommodated in 3 bits
        public static readonly byte[] UsxVCodeLookup = new byte[]
        {
            (1 << 5) + 0,  (1 << 5) + 0,  (2 << 5) + 1,  (2 << 5) + 2,
            (3 << 5) + 3,  (3 << 5) + 4,  (3 << 5) + 5,  (3 << 5) + 6,
            (3 << 5) + 7,  (3 << 5) + 7,  (4 << 5) + 8,  (4 << 5) + 9,
            (5 << 5) + 10, (5 << 5) + 10, (5 << 5) + 11, (5 << 5) + 11,
            (5 << 5) + 12, (5 << 5) + 12, (6 << 5) + 13, (6 << 5) + 14,
            (6 << 5) + 15, (6 << 5) + 15, (6 << 5) + 16, (6 << 5) + 16,
            (6 << 5) + 17, (6 << 5) + 17, (7 << 5) + 18, (7 << 5) + 19,
            (7 << 5) + 20, (7 << 5) + 21, (7 << 5) + 22, (7 << 5) + 23,
            (7 << 5) + 24, (7 << 5) + 25, (7 << 5) + 26, (7 << 5) + 27,
        };

        public static readonly byte[] LenMasks = UsxMask;

        // usx_code_94: character (33..126) -> (hcode<<5)|vcode. The equivalent of init_coder(), done in a static initializer.
        public static readonly byte[] UsxCode94 = BuildUsxCode94();

        private static byte[] BuildUsxCode94()
        {
            var table = new byte[94];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 28; j++)
                {
                    byte c = UsxSets[i][j];
                    if (c > 32)
                    {
                        table[c - UsxOffset94] = (byte)((i << 5) + j);
                        if (c >= 'a' && c <= 'z')
                            table[c - UsxOffset94 - ('a' - 'A')] = (byte)((i << 5) + j);
                    }
                }
            }
            return table;
        }

        // --- Horizontal codes (USX_HCODES_* / USX_HCODE_LENS_*) ---

        public static readonly HCodes HCodesDefault = new HCodes((0x00, 2), (0x40, 2), (0x80, 2), (0xC0, 3), (0xE0, 3));

        public static readonly HCodes HCodesAlphaOnly = new HCodes();

        public static readonly HCodes HCodesAlphaNumOnly = new HCodes((0x00, 1), null, (0x80, 1), null, null);

        public static readonly HCodes HCodesAlphaNumSymOnly = new HCodes((0x00, 1), (0x80, 2), (0xC0, 2), null, null);

        public static readonly HCodes HCodesFavorAlpha = new HCodes((0x00, 1), (0x80, 3), (0xA0, 3), (0xC0, 3), (0xE0, 3));

        public static readonly HCodes HCodesFavorDict = new HCodes((0x00, 2), (0x40, 2), (0xC0, 3), (0x80, 2), (0xE0, 3));

        public static readonly HCodes HCodesFavorSym = new HCodes((0x80, 3), (0x00, 1), (0xA0, 3), (0xC0, 3), (0xE0, 3));

        public static readonly HCodes HCodesFavorUmlaut = new HCodes((0x80, 3), (0xA0, 3), (0xC0, 3), (0xE0, 3), (0x00, 1));

        public static readonly HCodes HCodesNoDict = new HCodes((0x00, 2), (0x40, 2), (0x80, 2), null, (0xC0, 2));

        public static readonly HCodes HCodesNoUni = new HCodes((0x00, 2), (0x40, 2), (0x80, 2), (0xC0, 2), null);

        // --- Frequent sequences (USX_FREQ_SEQ_*) ---

        public static readonly string[] FreqSeqDefault = { "\": \"", "\": ", "</", "=\"", "\":\"", "://" };
        public static readonly string[] FreqSeqText = { " the ", " and ", "tion", " with", "ing", "ment" };
        public static readonly string[] FreqSeqUrl = { "https://", "www.", ".com", "http://", ".org", ".net" };
        public static readonly string[] FreqSeqJson = { "\": \"", "\": ", "\",", "}}}", "\":\"", "}}" };
        public static readonly string[] FreqSeqHtml = { "</", "=\"", "div", "href", "class", "<p>" };
        public static readonly string[] FreqSeqXml = { "</", "=\"", "\">", "<?xml version=\"1.0\"", "xmlns:", "://" };

    }
}
