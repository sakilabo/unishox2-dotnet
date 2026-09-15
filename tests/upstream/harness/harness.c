/*
 * Sakilabo.Unishox2 テスト用ハーネス。
 *
 * siara-cc/Unishox2 unishox2.c/.h を変更せずに呼び出すだけの薄いラッパー。
 * C# 側のテスト(NativeInteropTests)から標準入出力経由で呼び出し、
 * 圧縮バイト列の一致・相互展開・lines の自己参照を検証する。
 *
 * プロトコル(1行 1 リクエスト、標準入力から読み、標準出力へ 1 行で応答):
 *
 *   C <preset> <hex>
 *     単一要素を圧縮する(lines機能なし)。応答: <hex of compressed bytes> または ERR
 *
 *   D <preset> <hex>
 *     単一要素を展開する(lines機能なし)。応答: <hex of decompressed bytes> または ERR
 *
 *   L <preset> <hex0> <hex1> ... <hexN-1>
 *     複数要素を lines 機能で圧縮する。siara-cc/Unishox2の本来の呼び方通り、
 *     各要素を圧縮する時点でその要素自身を連結リストの先頭に追加してから呼ぶ
 *     (=自己参照を含む)。応答: 圧縮された各要素を空白区切りの hex で N 個。
 *
 *   M <preset> <hex0> <hex1> ... <hexN-1>
 *     複数要素を lines 機能で展開する。実運用を模し、複合側は「現在展開中の要素の
 *     原文を知らない」状態で呼ぶ(ノードの data は空バッファとして渡し、展開結果で
 *     後から埋める)。応答: 展開された各要素の hex を N 個(空白区切り)。
 *     siara-cc/Unishox2の decodeRepeat() が memmove() に依存しているため、このコマンドは
 *     自己参照の「重なりコピー」を伴う入力でsiara-cc/Unishox2のバグが顕在化することがある
 *     (期待通り展開できない場合、その要素の hex の代わりに ERR を返す)。
 *
 *   X <preset> <hexFreq0> <hexFreq1> <hexFreq2> <hexFreq3> <hexFreq4> <hexFreq5> <hex>
 *     preset の hcodes/hcode_lens/templates はそのままに、頻出文字列だけを
 *     6 個の hex(UTF-8 バイト列)で差し替えて圧縮する(lines機能なし)。
 *     日本語等の非 ASCII な頻出文字列を独自指定した場合の検証に使う。
 *
 *   Y <preset> <hexFreq0> ... <hexFreq5> <hex>
 *     X と同じ頻出文字列の差し替えで展開する(lines機能なし)。
 */

/*
 * MSVC 対応:
 * - sscanf() は境界を "%2x" で明示しており安全だが、MSVC は非推奨警告(C4996)を出すため
 *   標準ヘッダを include する前に _CRT_SECURE_NO_WARNINGS を定義して抑制する。
 * - strtok_r() は POSIX 関数で MSVC の CRT には無い。MSVC の strtok_s() は
 *   (str, delim, context) の順で strtok_r() と同じ引数構成のため、そのまま置き換える。
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
 * 注意: USX_HCODES_* / USX_HCODE_LENS_* / USX_TEMPLATES マクロは
 * (const unsigned char[]){...} 等の複合リテラル(compound literal)であり、これを
 * 関数内のローカル変数に代入して関数の外(呼び出し元)へポインタを返すと、
 * 戻り先では複合リテラルの生存期間が終わるため、値を translation unit
 * スコープの static 配列として保持する。
 * unishox2.h の対応するマクロ定義と 1:1 で書き写した値。
 */
static const uint8_t k_hcodes[17][5] = {
  /*  0 DFLT               */ {0x00, 0x40, 0x80, 0xC0, 0xE0},
  /*  1 ALPHA_ONLY         */ {0x00, 0x00, 0x00, 0x00, 0x00},
  /*  2 ALPHA_NUM_ONLY     */ {0x00, 0x00, 0x80, 0x00, 0x00},
  /*  3 ALPHA_NUM_SYM_ONLY */ {0x00, 0x80, 0xC0, 0x00, 0x00},
  /*  4 ALPHA_NUM_SYM_ONLY (favor text は同じ hcodes) */ {0x00, 0x80, 0xC0, 0x00, 0x00},
  /*  5 FAVOR_ALPHA        */ {0x00, 0x80, 0xA0, 0xC0, 0xE0},
  /*  6 FAVOR_DICT         */ {0x00, 0x40, 0xC0, 0x80, 0xE0},
  /*  7 FAVOR_SYM          */ {0x80, 0x00, 0xA0, 0xC0, 0xE0},
  /*  8 FAVOR_UMLAUT       */ {0x80, 0xA0, 0xC0, 0xE0, 0x00},
  /*  9 NO_DICT            */ {0x00, 0x40, 0x80, 0x00, 0xC0},
  /* 10 NO_UNI             */ {0x00, 0x40, 0x80, 0xC0, 0x00},
  /* 11 NO_UNI (favor text は同じ hcodes) */ {0x00, 0x40, 0x80, 0xC0, 0x00},
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

/* freq_seq はsiara-cc/Unishox2でグローバル配列(extern const char * USX_FREQ_SEQ_DFLT[]; 等)として
   定義されており、複合リテラルではない。そのままポインタを使ってよい。 */
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

/* USX_TEMPLATES の内容(文字列リテラルへのポインタ)を static 配列へ書き写す。
   文字列リテラル自体はプログラム全体で有効なので、ポインタ値のコピーで十分。 */
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

/* freq_seq を hex 引数 6 個から差し替えて圧縮/展開する。カスタム UTF-8 頻出文字列の検証用。 */
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
    elems[i][len] = 0; /* NUL 終端(siara-cc/Unishox2 us_lnk_lst.data は char* 前提) */

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

    /* 実運用を模し、現在要素のノードは「これから書き込む空バッファ」として渡す */
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
