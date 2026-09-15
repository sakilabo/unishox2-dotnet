# Sakilabo.Unishox2

English version: [README.md](./README.md)

本ライブラリは、[Unishox2](https://github.com/siara-cc/Unishox2) の成果に基づく C# 再実装です。短文向けの圧縮アルゴリズムとその実装を公開してくださった Arundale Ramanathan 氏、James Z. M. Gao 氏をはじめ、Unishox の開発・改良に携わる皆様に感謝します。

Sakilabo.Unishox2 は、短いテキストを圧縮し、元の文字列へ復元する C# ライブラリです。メッセージ、URL、JSON、ログなどのテキストを保存・送信する用途に使えます。日本語や絵文字を含む Unicode 文字列に対応しています。

短文向けの圧縮アルゴリズム **Unishox2** を C# で再実装しています。Unishox2 は、文字の種類、繰り返し、頻出文字列、日付などの定型パターンを使ってテキストデータを圧縮します。アルゴリズムの原典と C 実装は [siara-cc/Unishox2](https://github.com/siara-cc/Unishox2) で公開されています。

## できること

- **文字列の圧縮・復元**：`string` または UTF-8 の `byte[]` を圧縮し、`string` に復元します。
- **文字列配列の圧縮**：要素内・要素間の繰り返しを利用して文字列配列を圧縮・復元します。
- **内容に合わせた設定**：JSON、URL、XML などの定義済み設定を選べます。頻出文字列や定型パターンも指定できます。
- **圧縮結果の保存**：圧縮データを `byte[]` として取得し、保存・送信できます。

ライブラリは **.NET Standard 2.0** に対応しています。利用時に C コンパイラやネイティブライブラリは必要ありません。siara-cc/Unishox2 と圧縮データを交換する場合は、[互換性・非互換性](#siara-ccunishox2-との互換性非互換性)を確認してください。

## CompressOptions.Default での圧縮例

`siara-cc/Unishox2` の README に掲載されている12言語の圧縮例と同じ入力を、`Unishox2.Compress(text)`（`CompressOptions.Default`）で圧縮した実測値です。

| 言語 | 入力文字列 | UTF-16（バイト） | UTF-8（バイト） | 圧縮後（バイト） |
| --- | --- | --- | --- | --- |
| 英語 | Beauty is not in the face. Beauty is a light in the heart. | 116 | 58 | 30 |
| アラビア語 | الجمال ليس في الوجه. الجمال هو النور الذي في القلب. | 102 | 91 | 46 |
| ドイツ語 | Schönheit ist nicht im Gesicht. Schönheit ist ein Licht im Herzen. | 132 | 68 | 36 |
| スペイン語 | La belleza no está en la cara. La belleza es una luz en el corazón. | 134 | 69 | 38 |
| フランス語 | La beauté est pas dans le visage. La beauté est la lumière dans le coeur. | 146 | 76 | 39 |
| ヒンディー語 | सुंदरता चेहरे में नहीं है। सौंदर्य हृदय में प्रकाश है। | 108 | 144 | 53 |
| イタリア語 | La bellezza non è in faccia. La bellezza è la luce nel cuore. | 122 | 63 | 36 |
| 日本語 | 美は顔にありません。美は心の中の光です。 | 40 | 60 | 39 |
| ベンガル語 | সৌন্দর্য মুখে নেই। সৌন্দর্য হৃদয় একটি আলো। | 86 | 117 | 41 |
| ポルトガル語 | A beleza não está na cara. A beleza é a luz no coração. | 110 | 60 | 36 |
| ロシア語 | Красота не в лицо. Красота - это свет в сердце. | 94 | 82 | 44 |
| 中国語 | 美是不是在脸上。 美是心中的亮光。 | 34 | 49 | 36 |

入力文は siara-cc/Unishox2 の公式テストケースから取得しています。

### .NET 標準の圧縮方式との比較

[LICENSE-UPL.txt](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE-UPL.txt) を圧縮した結果です。Unishox2 は `CompressOptions.Default`、.NET 標準の各方式は .NET 10.0.11 の `CompressionLevel.Optimal` を使用しています。

| 方式 | バイト数 |
| --- | --- |
| 圧縮前 | 1,833 |
| Unishox2 | 1,077 |
| Deflate | 1,002 |
| GZip | 1,020 |
| Brotli | 965 |
| ZLib | 1,008 |

## 導入

現在は開発版 `0.1.0` で、NuGet.org には未公開です。ソースから [NuGet パッケージを生成](#nuget-パッケージの生成)し、利用するアプリのプロジェクトフォルダーで追加してください。

```sh
dotnet add package Sakilabo.Unishox2 --version 0.1.0 --source "<生成したartifactsフォルダーの絶対パス>"
```

## 使い方

### 文字列を圧縮して保存する

```csharp
using System.IO;
using Sakilabo.Unishox2;

public static void SaveText(string path, string text)
{
    byte[] compressed = Unishox2.Compress(text);
    File.WriteAllBytes(path, compressed);
}
```

### 保存済みのデータを読み出す

```csharp
public static string LoadText(string path)
{
    byte[] saved = File.ReadAllBytes(path);
    return Unishox2.Decompress(saved);
}
```

### UTF-8 のファイルを圧縮する

```csharp
byte[] utf8 = File.ReadAllBytes("message.txt");
byte[] compressed = Unishox2.Compress(utf8);
File.WriteAllBytes("message.usx", compressed);
```

`Compress` は `string` と UTF-8 の `byte[]` を受け付けます。`Decompress` の戻り値は `string` です。

### 文字列配列を圧縮する

`CompressLines` は、同じ要素内や前の要素の繰り返しを利用して文字列配列を圧縮します。

```csharp
public static byte[][] CompressMessages(string[] messages)
{
    return Unishox2.CompressLines(messages);
}
```

### 圧縮された配列を復元する

```csharp
public static string[] DecompressMessages(byte[][] data)
{
    return Unishox2.DecompressLines(data);
}
```

### オプションを指定する

圧縮・展開の設定は `CompressOption` で指定します。基本的な設定は `CompressOptions` に定義されています。

- `Default`：汎用。入力の種類を限定しない場合に使用。
- `AlphaOnly`：英字のみのテキスト向け。
- `AlphaNumOnly`：英数字のテキスト向け。
- `AlphaNumSymOnly`：英数字と記号のテキスト向け。
- `AlphaNumSymOnlyText`：`AlphaNumSymOnly` と同じ設定。参照元の `USX_PSET_ALPHA_NUM_SYM_ONLY_TXT` との名前の対応を保つために提供。
- `FavorAlpha`：英字が多いテキスト向け。
- `FavorDict`：繰り返しが多いテキスト向け。
- `FavorSym`：記号が多いテキスト向け。
- `FavorUmlaut`：ウムラウトなどの文字が多いテキスト向け。
- `NoDict`：繰り返しがないテキスト向け。
- `NoUnicode`：ASCII のテキスト向け。
- `NoUnicodeFavorText`：ASCII の文章向け。
- `Url`：URL 向け。
- `Json`：JSON 向け。
- `JsonNoUnicode`：ASCII の JSON 向け。
- `Xml`：XML 向け。
- `Html`：HTML 向け。

符号や頻出文字列などの具体的な値は [Tables.cs](https://github.com/sakilabo/unishox2-dotnet/blob/main/src/Sakilabo.Unishox2/Internal/Tables.cs) を参照してください。

各プロパティは、その設定で初期化した新しい `CompressOption` を返します。`new CompressOption()` は `CompressOptions.Default` と同じ設定になります。

作成した `CompressOption` を `Compress` の第2引数に渡します。JSON 用の定義済み設定を使って保存する例です。

```csharp
CompressOption options = CompressOptions.Json;
byte[] compressed = Unishox2.Compress("{\"status\":\"ready\"}", options);
File.WriteAllBytes("status.usx", compressed);
```

保存したデータを読み出すときは、圧縮時と同じ設定を `Decompress` の第2引数に渡します。

```csharp
byte[] data = File.ReadAllBytes("status.usx");
CompressOption options = CompressOptions.Json;
string json = Unishox2.Decompress(data, options);
```

`CompressLines` と `DecompressLines` も、第2引数に `CompressOption` を指定できます。これら4つのメソッドで第2引数を省略すると、`CompressOptions.Default` と同じ設定で処理します。

設定は圧縮データに含まれていません。`CompressOption` はシリアライズ可能です。

| 設定 | 用途 |
| --- | --- |
| `FrequentSequences` | よく出現する文字列を `string[]` で最大6個指定。末尾の未使用要素は省略可能。`null`・空文字列は `ArgumentException`。日本語も使用可能。 |
| `Templates` | 日付や時刻などの定型パターンを最大5個指定。`Templates.IsoDate` などの定数を使用可能。 |
| `HCodes` | 5文字グループそれぞれの水平符号とビット長を指定。使用しないグループは `null`。 |

生成した `CompressOption` の各設定は変更できます。例えば、独自の頻出文字列は次のように指定します。

```csharp
var options = new CompressOption
{
    FrequentSequences = new[]
    {
        "started", "completed", "waiting", "connected", "disconnected", "retrying"
    }
};
byte[] compressed = Unishox2.Compress("started: connected, completed", options);
File.WriteAllBytes("result.usx", compressed);
```

`AlphaOnly` など入力の文字種を限定する設定で、表現できない文字が渡された場合は `UnishoxFormatException` を送出します。

## siara-cc/Unishox2 との互換性・非互換性

文字列の途中にある UUID も専用形式で圧縮します。siara-cc/Unishox2 との相互展開を確認しています。

### lines の自己参照における実装差

lines で自己参照を使うデータのうち、復元時の参照先がまだ出力されていない範囲まで伸びる場合について、Sakilabo.Unishox2 は逐次コピーで復元します。

この不具合に該当する自己参照を含むデータの扱いは、次のとおりです。

| 圧縮した実装 | `siara-cc/Unishox2` で復元 | Sakilabo.Unishox2 で復元 |
| --- | --- | --- |
| `siara-cc/Unishox2` | 正しく復元できない | 復元できる |
| Sakilabo.Unishox2 | 正しく復元できない | 復元できる |

圧縮形式の変更はありません。具体的な比較結果は表のとおりです。

対象は、本リポジトリで参照している `siara-cc/Unishox2` の C 実装です。該当コードと検証内容は[C 実装との比較資料](https://github.com/sakilabo/unishox2-dotnet/blob/main/tests/upstream/c-implementation-comparison.ja.md)に記載しています。

### lines の NUL 文字（非互換）

NUL を通常のデータとして扱い、lines の圧縮・展開の参照処理でも要素全体を対象とします。`siara-cc/Unishox2` は参照処理に `strlen()` を使うため、NUL 以降への参照を含むデータを同実装で復元できない場合があります。

lines を使わない通常の圧縮でも NUL を扱えます。

## ビルドと通常テスト

**必要なもの：.NET 10 SDK。C コンパイラや siara-cc/Unishox2 の submodule の取得は不要です。**

以下のコマンドは、すべてこの README があるリポジトリのルートで実行します。

```sh
dotnet build Sakilabo.Unishox2.sln -c Release
dotnet test tests/Sakilabo.Unishox2.Tests/Sakilabo.Unishox2.Tests.csproj -c Release
```

通常テストは C# の圧縮・展開、オプション、圧縮データの入出力を検証します。siara-cc/Unishox2 との比較テストはスキップされ、C コードのビルドや実行は行いません。

VS Code では、このフォルダーを開いて `build`、`test`、`pack` タスクを使えます。`Ctrl+Shift+B` は `build` を実行します。通常の CI も C# のビルドと通常テストを実行します。

ライブラリ本体のターゲットは `netstandard2.0`、テストプロジェクトのターゲットは `net10.0` です。

## siara-cc/Unishox2 との互換性テスト

**siara-cc/Unishox2 との相互展開を確認するときだけ実行する追加テストです。この実行には C コンパイラが必要です。**

### 前提条件

通常開発用の .NET 10 SDK に加えて、Git と次の C ビルド環境を用意します。

| OS | 必要な C ビルド環境 |
| --- | --- |
| Windows | Visual Studio 2022 または Build Tools の「C++ によるデスクトップ開発」。MSVC x64 ツールと Windows SDK を含める。 |
| Linux | `gcc` と C 標準ライブラリの開発用ヘッダー。 |

Windows ではテストが `vswhere.exe` で Visual Studio を検出し、`VsDevCmd.bat` でビルド環境を設定します。通常の PowerShell や VS Code のターミナルから実行できます。

### 実行手順

リポジトリのルートで、まず比較対象の siara-cc/Unishox2 のソースを取得します。

```sh
git submodule update --init --recursive
```

次に、比較テストを有効にして実行します。

```sh
dotnet test tests/Sakilabo.Unishox2.Tests/Sakilabo.Unishox2.Tests.csproj -c Release -p:RequireNativeHarness=true
```

このコマンドは通常テストに加えて、次の処理を行います。

1. `tests/upstream/Unishox2/unishox2.c` と `tests/upstream/harness/harness.c` を、Windows では MSVC、Linux では GCC で自動ビルドする。
2. 作成した C 実行ファイルを起動する。
3. C# で圧縮したデータの C での展開と、C で圧縮したデータの C# での展開を検証する。

C ソースを手動でビルドする必要はありません。コンパイラや siara-cc/Unishox2 のソースが見つからない場合、またはビルド・相互展開に失敗した場合は、テストが失敗します。圧縮バイト列の完全一致や圧縮サイズの優劣は判定しません。[非互換部分](#siara-ccunishox2-との互換性非互換性)は Sakilabo.Unishox2 で原文を復元できることを検証します。

C 実行ファイルは比較テスト専用です。ライブラリ本体と NuGet パッケージには含まれていません。

コンパイラ別のビルド設定と比較内容は [C 実装との比較資料](https://github.com/sakilabo/unishox2-dotnet/blob/main/tests/upstream/c-implementation-comparison.ja.md) を参照してください。

## NuGet パッケージの生成

リポジトリのルートで実行します。C コンパイラは不要です。

```sh
dotnet pack src/Sakilabo.Unishox2/Sakilabo.Unishox2.csproj -c Release -o artifacts
```

`artifacts/` に `.nupkg` とシンボルパッケージ `.snupkg` を生成します。

利用するプロジェクトへの追加方法は [導入](#導入) を参照してください。

## siara-cc/Unishox2 のコードの更新

比較用の siara-cc/Unishox2 のコードは `tests/upstream/Unishox2` の git submodule で管理します。

1. siara-cc/Unishox2 のソースを取得する。

   ```sh
   git submodule update --init --recursive
   ```

2. Python 3 で公式テストケースを再生成し、テストプロジェクトへコピーする。

   ```sh
   python tests/upstream/extract_testcases.py
   python -c "from shutil import copyfile; copyfile('tests/upstream/testcases.json', 'tests/Sakilabo.Unishox2.Tests/UpstreamTestCases.json')"
   ```

3. [siara-cc/Unishox2 との互換性テスト](#siara-ccunishox2-との互換性テスト)を実行する。
4. この README と [C 実装との比較資料](https://github.com/sakilabo/unishox2-dotnet/blob/main/tests/upstream/c-implementation-comparison.ja.md) の確認結果を更新する。

## ライセンス

- siara-cc/Unishox2 に基づく圧縮・展開処理とデータテーブル：[Apache License 2.0](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE-APACHE.txt)。
- 新規に作成した C# API・ラッパー部分：[UPL 1.0](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE-UPL.txt)。Copyright 2026 株式会社さきラボ。

適用ライセンスと siara-cc/Unishox2 を含む著作権表記の詳細は [LICENSE](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE) を参照してください。
