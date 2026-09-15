# tests/upstream

英語版は [c-implementation-comparison.md](./c-implementation-comparison.md) にあります。

Sakilabo.Unishox2 のテスト・検証専用に配置している、外部資料(siara-cc/Unishox2)と検証ツールです(siara-cc/Unishox2 そのものではなく、Sakilabo.Unishox2 独自の検証資料)。

- `Unishox2/` — [siara-cc/Unishox2](https://github.com/siara-cc/Unishox2) の git submodule。初回クローン後は `git submodule update --init --recursive` で取得する。ライセンスは Apache License 2.0(著作権表示 `Copyright (C) 2020 Siara Logics (cc)`)。Sakilabo.Unishox2 のソースコードは変更していない。
- `harness/harness.c` — siara-cc/Unishox2 の `unishox2.c`/`.h` を変更せず呼び出すだけの、Sakilabo.Unishox2 独自のテスト用 CLI。標準入出力で 1 行 1 リクエストのプロトコルを実装する(コマンド一覧はファイル冒頭のコメント)。Copyright 2026 株式会社さきラボ、UPL-1.0。
- `extract_testcases.py` / `testcases.json` — siara-cc/Unishox2 の `test_unishox2.c` の `run_unit_tests()` からテストケース文字列を抽出するツールと、その結果(159 件、NUL バイトを含む 1 件を除く)。`tests/Sakilabo.Unishox2.Tests/UpstreamTestCases.json` はこの結果をテストプロジェクトへコピーしたもの。

siara-cc/Unishox2 との比較は `dotnet test -p:RequireNativeHarness=true` で明示した場合だけ実行する。WindowsはMSVC、Linuxはgccでハーネスと siara-cc/Unishox2 のコードをビルドし、相互展開を検証する。

通常のテストはOSにかかわらずC#のみで実行する。Cコンパイラも siara-cc/Unishox2 の submoduleも不要。C 実装との比較を指定しなければ該当16件をスキップし、コンパイラの検出・ビルドも行わない。比較を指定した場合にビルド・実行できなければ、診断情報を含むテスト失敗として扱う。

実行手順と前提条件は [README の互換性テスト](../../README.ja.md#siara-ccunishox2-との互換性テスト)を参照。

## lines 機能における NUL バイトの扱い（非互換）

Sakilabo.Unishox2 は NUL を終端扱いせず、圧縮・展開の参照処理でも要素全体を対象とする。圧縮側の `CompressLineChain.GetLength` は配列長、展開側の `DecompressLineChain.GetLength` は現在の出力長または過去要素の配列長を返す。

siara-cc/Unishox2 の `matchLine()` と `decodeRepeat()` は、現在要素（ctx=0）と過去要素のどちらにも `strlen()` を使う。NUL 以降への参照を含む Sakilabo.Unishox2 の圧縮データは、siara-cc/Unishox2 で復元できない場合がある。これは許容する非互換性であり、圧縮形式自体は変更しない。[README の非互換性](../../README.ja.md#lines-の-nul-文字非互換)を参照。

`NativeLinesNulByteTests.cs` は siara-cc/Unishox2 が圧縮した NUL 含有データを C# で復元できることを検証する。NUL 以降への参照を含む C# データについて siara-cc/Unishox2 での復元は要求せず、siara-cc/Unishox2 の失敗も合格条件にしない。C# の参照検索・自己参照・過去要素参照・保存後復元は通常のマネージドテストで確認する。

## siara-cc/Unishox2 との実装差

比較テストで確認した実装差と、Sakilabo.Unishox2 側の扱いを記録します。

### 1. lines の自己参照におけるコピー方法

- 圧縮と復元の確認結果: Sakilabo.Unishox2 は、siara-cc/Unishox2 と C# のどちらが圧縮した該当データも復元できる。
- 該当箇所: `unishox2.c` の `decodeRepeat()`(`memmove(out + ol, cur_line->data + dist, min_of(left, dict_len))`)。ctx=0 の自己参照では `cur_line->data` は `out` と同じバッファであり、これは実質 `memmove(out + ol, out + dist, min_of(left, dict_len))` になる。
- 再現条件: `dist` は(現在位置からの後方距離ではなく)要素の先頭からの絶対位置(コピー元の開始インデックス)である。lines 機能で、要素内に短周期の反復(例: `"XAXAXAXA..."`)があり、圧縮側の `matchLine()` が自己参照(ctx=0)として、コピー元の範囲がまだ書き込まれていない出力位置に重なる `dist + dict_len > ol`(`ol` は複合側の現在の出力位置)となる「重なりコピー」を生成する場合に発生する。
- 影響: 複合側が「現在展開中の要素の原文を保持していない」実運用パターン(バイト列だけを保存し、後で複合する一般的な使い方)で展開すると、その要素の展開結果が途中で欠落する。`memmove()` は「コピー元の呼び出し時点での値」を使う規格上の意味を持つため、まだ書き込まれていない領域を参照する重なりコピーを、逐次的な自己言及コピー(LZ77 的な意味)として機能させることはできない。
- 検証内容: 自動テストは、siara-cc/Unishox2 が圧縮したデータを C# で展開できることと C# の往復を確認する。
- Sakilabo.Unishox2 の対応: `Internal/LineMatching.cs` の `DecodeRepeat()`/`DecodeOccurance()` で、1 バイトずつ「直前に書いたばかりの値」を参照しながらコピーする実装にし、siara-cc/Unishox2 と同じビット列を正しく展開する(踏襲せず修正)。`LinesAndSelfReferenceTests.SelfReferencingOverlapWithinSingleElement_RoundTrips` で確認済み。

### 2. UUID（GUID）検出位置の違い

- 該当箇所: `unishox2.c` のメイン圧縮ループ内、UUID 検出の内側ループ(`if (c_uid == '-' && (uid_pos == 8 || uid_pos == 13 || uid_pos == 18 || uid_pos == 23))`)。
- 再現条件: 圧縮対象の文字列内で、UUID パターンが先頭(l==0)以外の位置に出現する場合。`uid_pos` は文字列全体の絶対インデックスであり、本来は `uid_pos - l`(UUID の先頭からの相対位置)と比較すべきところを、絶対位置と比較してしまっている。
- 影響: l>0 の位置にある UUID は専用の圧縮(4 ビット/nibble の高密度エンコード)が使われず、通常の HEX 検出やリテラルエンコードにフォールバックする。往復性(展開して元の文字列に戻ること)は失われず、圧縮率がやや落ちるだけ。
- 展開側への影響の有無: `unishox2.c` の展開側(UUID 専用エンコードのニブル列を読み出す箇所)はエスケープコード出現後の位置(l)を一切参照せず、常に 32 ニブルを固定長で読む。UUID 専用エンコードを選ぶかどうかは圧縮側だけの判断であり、生成されるビット列の構造は l==0 でも l>0 でも同一である。そのため圧縮側の判定を相対位置に修正しても、生成されるビット列を展開する側(無改造の siara-cc/Unishox2 の C 実装・Sakilabo.Unishox2 のいずれも)の互換性には影響しない。影響するのは「どちらのエンコードを選ぶか」による圧縮バイト列そのものだけである。
- Sakilabo.Unishox2 の対応: `uid_pos - l` で判定する。l>0 に UUID を含む入力では圧縮バイト列が異なる場合があるが、双方で相互に展開できることを `NativeUuidCompressionTests.cs` で確認している。

## 検証ツール自体(harness.c・NativeHarness.cs)の既知の注意点

上記は siara-cc/Unishox2 の `unishox2.c` 自体の挙動であり、以下は区別する。検証ツール(`harness.c`・`NativeHarness.cs`)側の制約や既知の注意点は、見つかり次第この節に追記する(現時点で記録すべき既知の問題はない)。

## C ハーネスのビルド設定

- Windows：`vswhere.exe` で `Microsoft.VisualStudio.Component.VC.Tools.x86.x64` を持つ Visual Studio を検出し、`VsDevCmd.bat -arch=x64 -host_arch=x64` を実行した後、`cl /TC /std:c11 /utf-8 /W3` でビルドする。
- Linux：`gcc -DUNISHOX_API_WITH_OUTPUT_LEN=1 -O2` でビルドする。
- 両環境とも `harness.c` と siara-cc/Unishox2 の `unishox2.c` をコンパイルし、siara-cc/Unishox2 のフォルダーをインクルードパスに指定する。siara-cc/Unishox2 のソース自体は変更しない。
- 独自ハーネスの MSVC 対応は `strtok_r` を `strtok_s` に置換するマクロと、`sscanf` の非推奨警告を抑制する `_CRT_SECURE_NO_WARNINGS`。
- MSVC では出力長付き API を有効にしない。siara-cc/Unishox2 の `#if (UNISHOX_API_OUT_AND_LEN(0,1)) == 0` がコンマを含む式に展開されると C1012 になるため、既定の出力長を省略する API を使用する。圧縮・展開結果の相互運用は MSVC 版で検証済み。

具体的な起動処理は [NativeHarness.cs](../Sakilabo.Unishox2.Tests/NativeHarness.cs) に実装している。
