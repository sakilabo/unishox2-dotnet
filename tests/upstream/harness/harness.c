/*
 * Test harness for Sakilabo.Unishox2.
 *
 * A thin wrapper that calls siara-cc/Unishox2 unishox2.c and unishox2.h without modifying them.
 * The C# tests (NativeInteropTests) drive it over standard input and output to verify matching
 * compressed bytes, cross-decompression, and lines self-references.
 *
 * Protocol: one request per line, read from standard input, answered with one line on standard output.
 *
 *   C <preset> <hex>
 *     Compress a single element, without the lines feature.
 *     Response: <hex of compressed bytes>, or ERR.
 *
 *   D <preset> <hex>
 *     Decompress a single element, without the lines feature.
 *     Response: <hex of decompressed bytes>, or ERR.
 *
 *   L <preset> <hex0> <hex1> ... <hexN-1>
 *     Compress several elements with the lines feature. Following the way siara-cc/Unishox2 is meant
 *     to be called, each element is prepended to the linked list before it is compressed, so
 *     self-references are included.
 *     Response: N space-separated hex strings, one per compressed element.
 *
 *   M <preset> <hex0> <hex1> ... <hexN-1>
 *     Decompress several elements with the lines feature. To match real use, the decoder is called
 *     without knowing the source text of the element being decompressed: the node data is passed as
 *     an empty buffer and filled in from the decompressed result.
 *     Response: N space-separated hex strings, one per decompressed element.
 *     Because decodeRepeat() in siara-cc/Unishox2 relies on memmove(), this command can expose the
 *     upstream bug for input involving an overlapping self-reference copy. When an element cannot be
 *     decompressed as expected, ERR is returned in place of its hex string.
 *
 *   X <preset> <hexFreq0> <hexFreq1> <hexFreq2> <hexFreq3> <hexFreq4> <hexFreq5> <hex>
 *     Compress without the lines feature, keeping the hcodes, hcode_lens and templates of the preset
 *     but replacing the frequent sequences with six hex values holding UTF-8 byte sequences.
 *     Used to verify custom non-ASCII frequent sequences, such as Japanese ones.
 *
 *   Y <preset> <hexFreq0> ... <hexFreq5> <hex>
 *     Decompress with the same frequent sequence replacement as X, without the lines feature.
 */

/*
 * MSVC support:
 * - sscanf() is bounded explicitly with "%2x" and is safe, but MSVC emits a deprecation warning
 *   (C4996), so _CRT_SECURE_NO_WARNINGS is defined before including the standard headers.
 * - strtok_r() is a POSIX function and is absent from the MSVC CRT. The MSVC strtok_s() takes
 *   (str, delim, context), the same argument layout as strtok_r(), so it is substituted directly.
 */
#ifdef _MSC_VER
#  define _CRT_SECURE_NO_WARNINGS
#endif

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include "unishox2.h"

#ifdef _MSC_VER
#  define strtok_r(str, delim, saveptr) strtok_s((str), (delim), (saveptr))
#endif

#define MAX_LINE 262144
#define MAX_ELEM 65536
#define MAX_ELEMS 64

static int hex_decode(const char *hex, uint8_t *out, int out_cap) {
  int n = (int)strlen(hex);
  if (n % 2 != 0) return -1;
  int len = n / 2;
  if (len > out_cap) return -1;
  for (int i = 0; i < len; i++) {
    unsigned int byte;
    if (sscanf(hex + i * 2, "%2x", &byte) != 1) return -1;
    out[i] = (uint8_t)byte;
  }
  return len;
}

static void hex_encode(const uint8_t *data, int len, char *out) {
  static const char *digits = "0123456789abcdef";
  for (int i = 0; i < len; i++) {
    out[i * 2] = digits[(data[i] >> 4) & 0xF];
    out[i * 2 + 1] = digits[data[i] & 0xF];
  }
  out[len * 2] = 0;
}

/*
 * Note: the USX_HCODES_*, USX_HCODE_LENS_* and USX_TEMPLATES macros are compound literals such as
 * (const unsigned char[]){...}. Assigning one to a local variable and returning the pointer to the
 * caller would outlive the compound literal, so the values are held instead in static arrays at
 * translation unit scope.
 * The values are transcribed one to one from the matching macro definitions in unishox2.h.
 */
static const uint8_t k_hcodes[17][5] = {
  /*  0 DFLT               */ {0x00, 0x40, 0x80, 0xC0, 0xE0},
  /*  1 ALPHA_ONLY         */ {0x00, 0x00, 0x00, 0x00, 0x00},
  /*  2 ALPHA_NUM_ONLY     */ {0x00, 0x00, 0x80, 0x00, 0x00},
  /*  3 ALPHA_NUM_SYM_ONLY */ {0x00, 0x80, 0xC0, 0x00, 0x00},
  /*  4 ALPHA_NUM_SYM_ONLY (favor text uses the same hcodes) */ {0x00, 0x80, 0xC0, 0x00, 0x00},
  /*  5 FAVOR_ALPHA        */ {0x00, 0x80, 0xA0, 0xC0, 0xE0},
  /*  6 FAVOR_DICT         */ {0x00, 0x40, 0xC0, 0x80, 0xE0},
  /*  7 FAVOR_SYM          */ {0x80, 0x00, 0xA0, 0xC0, 0xE0},
  /*  8 FAVOR_UMLAUT       */ {0x80, 0xA0, 0xC0, 0xE0, 0x00},
  /*  9 NO_DICT            */ {0x00, 0x40, 0x80, 0x00, 0xC0},
  /* 10 NO_UNI             */ {0x00, 0x40, 0x80, 0xC0, 0x00},
  /* 11 NO_UNI (favor text uses the same hcodes) */ {0x00, 0x40, 0x80, 0xC0, 0x00},
  /* 12 URL (DFLT)         */ {0x00, 0x40, 0x80, 0xC0, 0xE0},
  /* 13 JSON (DFLT)        */ {0x00, 0x40, 0x80, 0xC0, 0xE0},
  /* 14 JSON_NO_UNI        */ {0x00, 0x40, 0x80, 0xC0, 0x00},
  /* 15 XML (DFLT)         */ {0x00, 0x40, 0x80, 0xC0, 0xE0},
  /* 16 HTML (DFLT)        */ {0x00, 0x40, 0x80, 0xC0, 0xE0},
};

static const uint8_t k_hcode_lens[17][5] = {
  /*  0 DFLT               */ {2, 2, 2, 3, 3},
  /*  1 ALPHA_ONLY         */ {0, 0, 0, 0, 0},
  /*  2 ALPHA_NUM_ONLY     */ {1, 0, 1, 0, 0},
  /*  3 ALPHA_NUM_SYM_ONLY */ {1, 2, 2, 0, 0},
  /*  4 ALPHA_NUM_SYM_ONLY */ {1, 2, 2, 0, 0},
  /*  5 FAVOR_ALPHA        */ {1, 3, 3, 3, 3},
  /*  6 FAVOR_DICT         */ {2, 2, 3, 2, 3},
  /*  7 FAVOR_SYM          */ {3, 1, 3, 3, 3},
  /*  8 FAVOR_UMLAUT       */ {3, 3, 3, 3, 1},
  /*  9 NO_DICT            */ {2, 2, 2, 0, 2},
  /* 10 NO_UNI             */ {2, 2, 2, 2, 0},
  /* 11 NO_UNI             */ {2, 2, 2, 2, 0},
  /* 12 URL (DFLT)         */ {2, 2, 2, 3, 3},
  /* 13 JSON (DFLT)        */ {2, 2, 2, 3, 3},
  /* 14 JSON_NO_UNI        */ {2, 2, 2, 2, 0},
  /* 15 XML (DFLT)         */ {2, 2, 2, 3, 3},
  /* 16 HTML (DFLT)        */ {2, 2, 2, 3, 3},
};

/* In siara-cc/Unishox2 freq_seq is defined as a global array, such as
   extern const char * USX_FREQ_SEQ_DFLT[], and not as a compound literal, so the pointer can be
   used directly. */
static const char *const *k_freq_seq_for_preset(int preset) {
  switch (preset) {
    case 1: case 5: case 11: return USX_FREQ_SEQ_TXT;
    case 12: return USX_FREQ_SEQ_URL;
    case 13: case 14: return USX_FREQ_SEQ_JSON;
    case 15: return USX_FREQ_SEQ_XML;
    case 16: return USX_FREQ_SEQ_HTML;
    default: return USX_FREQ_SEQ_DFLT;
  }
}

/* Transcribe the contents of USX_TEMPLATES, which are pointers to string literals, into a static
   array. The string literals themselves live for the whole program, so copying the pointer values
   is enough. */
static const char *g_templates_buf[5] = {
  "tfff-of-tfTtf:rf:rf.fffZ", "tfff-of-tf", "(fff) fff-ffff", "tf:rf:rf", NULL,
};

static int get_preset(int preset, const uint8_t **hcodes, const uint8_t **hcode_lens, const char ***freq_seq, const char ***templates) {
  if (preset < 0 || preset > 16)
    return 0;
  *hcodes = k_hcodes[preset];
  *hcode_lens = k_hcode_lens[preset];
  *freq_seq = (const char **)k_freq_seq_for_preset(preset);
  *templates = g_templates_buf;
  return 1;
}

static void handle_single(char cmd, int preset, char *hexarg) {
  const uint8_t *hcodes, *hcode_lens;
  const char **freq_seq, **templates;
  static uint8_t inbuf[MAX_ELEM];
  static uint8_t outbuf[MAX_ELEM * 4];
  static char hexout[MAX_ELEM * 8];

  if (!get_preset(preset, &hcodes, &hcode_lens, &freq_seq, &templates)) {
    printf("ERR\n");
    return;
  }
  int inlen = hex_decode(hexarg, inbuf, sizeof inbuf);
  if (inlen < 0) {
    printf("ERR\n");
    return;
  }
  int outlen;
  if (cmd == 'C') {
    outlen = unishox2_compress_lines((char *)inbuf, inlen, UNISHOX_API_OUT_AND_LEN((char *)outbuf, (int)sizeof outbuf), hcodes, hcode_lens, freq_seq, templates, NULL);
  } else {
    outlen = unishox2_decompress_lines((char *)inbuf, inlen, UNISHOX_API_OUT_AND_LEN((char *)outbuf, (int)sizeof outbuf), hcodes, hcode_lens, freq_seq, templates, NULL);
  }
  if (outlen < 0 || outlen > (int)sizeof outbuf) {
    printf("ERR\n");
    return;
  }
  hex_encode(outbuf, outlen, hexout);
  printf("%s\n", hexout);
}

/* Compress and decompress with freq_seq replaced from six hex arguments, to verify custom UTF-8
   frequent sequences. */
static void handle_custom_freq(char cmd, int preset, char **hexargs) {
  const uint8_t *hcodes, *hcode_lens;
  const char **base_freq_seq, **templates;
  static char freq_bufs[6][MAX_ELEM];
  static const char *custom_freq_seq[6];
  static uint8_t inbuf[MAX_ELEM];
  static uint8_t outbuf[MAX_ELEM * 4];
  static char hexout[MAX_ELEM * 8];

  if (!get_preset(preset, &hcodes, &hcode_lens, &base_freq_seq, &templates)) {
    printf("ERR\n");
    return;
  }

  for (int i = 0; i < 6; i++) {
    int len = hex_decode(hexargs[i], (uint8_t *)freq_bufs[i], MAX_ELEM - 1);
    if (len < 0) {
      printf("ERR\n");
      return;
    }
    freq_bufs[i][len] = 0;
    custom_freq_seq[i] = freq_bufs[i];
  }

  int inlen = hex_decode(hexargs[6], inbuf, sizeof inbuf);
  if (inlen < 0) {
    printf("ERR\n");
    return;
  }

  int outlen;
  if (cmd == 'X') {
    outlen = unishox2_compress_lines((char *)inbuf, inlen, UNISHOX_API_OUT_AND_LEN((char *)outbuf, (int)sizeof outbuf), hcodes, hcode_lens, custom_freq_seq, templates, NULL);
  } else {
    outlen = unishox2_decompress_lines((char *)inbuf, inlen, UNISHOX_API_OUT_AND_LEN((char *)outbuf, (int)sizeof outbuf), hcodes, hcode_lens, custom_freq_seq, templates, NULL);
  }
  if (outlen < 0 || outlen > (int)sizeof outbuf) {
    printf("ERR\n");
    return;
  }
  hex_encode(outbuf, outlen, hexout);
  printf("%s\n", hexout);
}

static void handle_lines_compress(int preset, char **hexargs, int count) {
  const uint8_t *hcodes, *hcode_lens;
  const char **freq_seq, **templates;
  static uint8_t elems[MAX_ELEMS][MAX_ELEM];
  static uint8_t outbuf[MAX_ELEM * 4];
  static char hexout[MAX_ELEM * 8];

  if (!get_preset(preset, &hcodes, &hcode_lens, &freq_seq, &templates) || count > MAX_ELEMS) {
    printf("ERR\n");
    return;
  }

  struct us_lnk_lst *head = NULL;
  struct us_lnk_lst nodes[MAX_ELEMS];

  for (int i = 0; i < count; i++) {
    int len = hex_decode(hexargs[i], elems[i], MAX_ELEM);
    if (len < 0) {
      printf("ERR\n");
      return;
    }
    elems[i][len] = 0; /* NUL terminate; siara-cc/Unishox2 us_lnk_lst.data assumes char* */

    nodes[i].data = (char *)elems[i];
    nodes[i].previous = head;
    head = &nodes[i];

    int outlen = unishox2_compress_lines((char *)elems[i], len, UNISHOX_API_OUT_AND_LEN((char *)outbuf, (int)sizeof outbuf), hcodes, hcode_lens, freq_seq, templates, head);
    if (outlen < 0 || outlen > (int)sizeof outbuf) {
      printf("ERR\n");
      return;
    }
    hex_encode(outbuf, outlen, hexout);
    printf("%s", hexout);
    if (i + 1 < count) printf(" ");
  }
  printf("\n");
}

static void handle_lines_decompress(int preset, char **hexargs, int count) {
  const uint8_t *hcodes, *hcode_lens;
  const char **freq_seq, **templates;
  static uint8_t inbuf[MAX_ELEM];
  static char decoded[MAX_ELEMS][MAX_ELEM];
  static char hexout[MAX_ELEM * 8];

  if (!get_preset(preset, &hcodes, &hcode_lens, &freq_seq, &templates) || count > MAX_ELEMS) {
    printf("ERR\n");
    return;
  }

  struct us_lnk_lst *head = NULL;
  struct us_lnk_lst nodes[MAX_ELEMS];

  for (int i = 0; i < count; i++) {
    int inlen = hex_decode(hexargs[i], inbuf, sizeof inbuf);
    if (inlen < 0) {
      printf("ERR");
      if (i + 1 < count) printf(" ");
      continue;
    }

    /* To match real use, the node for the current element is passed as an empty buffer to be written into */
    memset(decoded[i], 0, MAX_ELEM);
    nodes[i].data = decoded[i];
    nodes[i].previous = head;

    int outlen = unishox2_decompress_lines((char *)inbuf, inlen, UNISHOX_API_OUT_AND_LEN(decoded[i], MAX_ELEM - 1), hcodes, hcode_lens, freq_seq, templates, &nodes[i]);
    if (outlen < 0 || outlen >= MAX_ELEM) {
      printf("ERR");
      decoded[i][0] = 0;
    } else {
      decoded[i][outlen] = 0;
      hex_encode((uint8_t *)decoded[i], outlen, hexout);
      printf("%s", hexout);
    }
    head = &nodes[i];
    if (i + 1 < count) printf(" ");
  }
  printf("\n");
}

int main(void) {
  static char line[MAX_LINE];
  while (fgets(line, sizeof line, stdin)) {
    size_t n = strlen(line);
    while (n > 0 && (line[n - 1] == '\n' || line[n - 1] == '\r')) line[--n] = 0;
    if (n == 0) continue;

    char *saveptr = NULL;
    char *tok = strtok_r(line, " ", &saveptr);
    if (!tok) { printf("ERR\n"); continue; }
    char cmd = tok[0];

    tok = strtok_r(NULL, " ", &saveptr);
    if (!tok) { printf("ERR\n"); continue; }
    int preset = atoi(tok);

    if (cmd == 'C' || cmd == 'D') {
      tok = strtok_r(NULL, " ", &saveptr);
      if (!tok) { printf("ERR\n"); continue; }
      handle_single(cmd, preset, tok);
    } else if (cmd == 'X' || cmd == 'Y') {
      char *args[7];
      int n = 0;
      while (n < 7 && (tok = strtok_r(NULL, " ", &saveptr)) != NULL) {
        args[n++] = tok;
      }
      if (n != 7) { printf("ERR\n"); continue; }
      handle_custom_freq(cmd, preset, args);
    } else if (cmd == 'L' || cmd == 'M') {
      char *hexargs[MAX_ELEMS];
      int count = 0;
      while (count < MAX_ELEMS && (tok = strtok_r(NULL, " ", &saveptr)) != NULL) {
        hexargs[count++] = tok;
      }
      if (count == 0) { printf("ERR\n"); continue; }
      if (cmd == 'L') handle_lines_compress(preset, hexargs, count);
      else handle_lines_decompress(preset, hexargs, count);
    } else {
      printf("ERR\n");
    }
    fflush(stdout);
  }
  return 0;
}
