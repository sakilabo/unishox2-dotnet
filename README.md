# Sakilabo.Unishox2

日本語版: [README.ja.md](./README.ja.md)

This library is a C# re-implementation based on the work of [Unishox2](https://github.com/siara-cc/Unishox2). Our thanks go to Arundale Ramanathan and James Z. M. Gao, who published the short-string compression algorithm and its implementation, and to everyone who has developed and improved Unishox.

Sakilabo.Unishox2 is a C# library that compresses short text and restores the original string. It is useful for storing and transmitting text such as messages, URLs, JSON and logs. It handles Unicode strings, including Japanese and emoji.

It re-implements **Unishox2**, a compression algorithm for short text, in C#. Unishox2 compresses text data using character classes, repetitions, frequent sequences and fixed patterns such as dates. The original algorithm and its C implementation are published at [siara-cc/Unishox2](https://github.com/siara-cc/Unishox2).

## What it does

- **Compress and restore strings**: compresses a `string` or a UTF-8 `byte[]`, and restores it to a `string`.
- **Compress string arrays**: compresses and restores an array of strings, exploiting repetitions within an element and across elements.
- **Settings matched to the content**: predefined settings for JSON, URLs, XML and more. Frequent sequences and fixed patterns can also be specified.
- **Store the compressed result**: the compressed data is returned as a `byte[]` that can be stored or transmitted.

The library targets **.NET Standard 2.0**. Using it requires no C compiler and no native library. If you exchange compressed data with siara-cc/Unishox2, see [Compatibility and incompatibilities](#compatibility-and-incompatibilities-with-siara-ccunishox2).

## Compression examples with CompressOptions.Default

Measured results of compressing the same 12-language inputs listed in the `siara-cc/Unishox2` README with `Unishox2.Compress(text)`, which uses `CompressOptions.Default`.

| Language | Input string | UTF-16 (bytes) | UTF-8 (bytes) | Compressed (bytes) |
| --- | --- | --- | --- | --- |
| English | Beauty is not in the face. Beauty is a light in the heart. | 116 | 58 | 30 |
| Arabic | الجمال ليس في الوجه. الجمال هو النور الذي في القلب. | 102 | 91 | 46 |
| German | Schönheit ist nicht im Gesicht. Schönheit ist ein Licht im Herzen. | 132 | 68 | 36 |
| Spanish | La belleza no está en la cara. La belleza es una luz en el corazón. | 134 | 69 | 38 |
| French | La beauté est pas dans le visage. La beauté est la lumière dans le coeur. | 146 | 76 | 39 |
| Hindi | सुंदरता चेहरे में नहीं है। सौंदर्य हृदय में प्रकाश है। | 108 | 144 | 53 |
| Italian | La bellezza non è in faccia. La bellezza è la luce nel cuore. | 122 | 63 | 36 |
| Japanese | 美は顔にありません。美は心の中の光です。 | 40 | 60 | 39 |
| Bengali | সৌন্দর্য মুখে নেই। সৌন্দর্য হৃদয় একটি আলো। | 86 | 117 | 41 |
| Portuguese | A beleza não está na cara. A beleza é a luz no coração. | 110 | 60 | 36 |
| Russian | Красота не в лицо. Красота - это свет в сердце. | 94 | 82 | 44 |
| Chinese | 美是不是在脸上。 美是心中的亮光。 | 34 | 49 | 36 |

The input sentences are taken from the official siara-cc/Unishox2 test cases.

### Comparison with the standard .NET compression methods

Results of compressing [LICENSE-UPL.txt](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE-UPL.txt). Unishox2 uses `CompressOptions.Default`, and each standard .NET method uses `CompressionLevel.Optimal` on .NET 10.0.11.

| Method | Bytes |
| --- | --- |
| Uncompressed | 1,833 |
| Unishox2 | 1,077 |
| Deflate | 1,002 |
| GZip | 1,020 |
| Brotli | 965 |
| ZLib | 1,008 |

## Installation

This is development version `0.1.0` and it is not published on NuGet.org yet. [Build the NuGet package](#building-the-nuget-package) from source, then add it in the project folder of the application that uses it.

```sh
dotnet add package Sakilabo.Unishox2 --version 0.1.0 --source "<absolute path of the artifacts folder you produced>"
```

## Usage

### Compress a string and save it

```csharp
using System.IO;
using Sakilabo.Unishox2;

public static void SaveText(string path, string text)
{
    byte[] compressed = Unishox2.Compress(text);
    File.WriteAllBytes(path, compressed);
}
```

### Read saved data back

```csharp
public static string LoadText(string path)
{
    byte[] saved = File.ReadAllBytes(path);
    return Unishox2.Decompress(saved);
}
```

### Compress a UTF-8 file

```csharp
byte[] utf8 = File.ReadAllBytes("message.txt");
byte[] compressed = Unishox2.Compress(utf8);
File.WriteAllBytes("message.usx", compressed);
```

`Compress` accepts a `string` or a UTF-8 `byte[]`. `Decompress` returns a `string`.

### Compress an array of strings

`CompressLines` compresses an array of strings by exploiting repetitions within the same element and in earlier elements.

```csharp
public static byte[][] CompressMessages(string[] messages)
{
    return Unishox2.CompressLines(messages);
}
```

### Restore a compressed array

```csharp
public static string[] DecompressMessages(byte[][] data)
{
    return Unishox2.DecompressLines(data);
}
```

### Specify options

Compression and decompression settings are given as a `CompressOption`. The basic sets of settings are defined on `CompressOptions`.

- `Default`: general purpose, for when the kind of input is not restricted.
- `AlphaOnly`: for text made up of letters only.
- `AlphaNumOnly`: for text made up of letters and digits.
- `AlphaNumSymOnly`: for text made up of letters, digits and symbols.
- `AlphaNumSymOnlyText`: the same settings as `AlphaNumSymOnly`, provided to keep the name aligned with `USX_PSET_ALPHA_NUM_SYM_ONLY_TXT` upstream.
- `FavorAlpha`: for text dominated by letters.
- `FavorDict`: for text with many repetitions.
- `FavorSym`: for text dominated by symbols.
- `FavorUmlaut`: for text with many umlauts and similar characters.
- `NoDict`: for text without repetitions.
- `NoUnicode`: for ASCII text.
- `NoUnicodeFavorText`: for ASCII prose.
- `Url`: for URLs.
- `Json`: for JSON.
- `JsonNoUnicode`: for ASCII JSON.
- `Xml`: for XML.
- `Html`: for HTML.

For the concrete values of the codes and frequent sequences, see [Tables.cs](https://github.com/sakilabo/unishox2-dotnet/blob/main/src/Sakilabo.Unishox2/Internal/Tables.cs).

Each property returns a new `CompressOption` initialized with those settings. `new CompressOption()` produces the same settings as `CompressOptions.Default`.

Pass the `CompressOption` you created as the second argument to `Compress`. Here is an example that saves data using the predefined settings for JSON.

```csharp
CompressOption options = CompressOptions.Json;
byte[] compressed = Unishox2.Compress("{\"status\":\"ready\"}", options);
File.WriteAllBytes("status.usx", compressed);
```

When reading the saved data back, pass the same settings as the second argument to `Decompress`.

```csharp
byte[] data = File.ReadAllBytes("status.usx");
CompressOption options = CompressOptions.Json;
string json = Unishox2.Decompress(data, options);
```

`CompressLines` and `DecompressLines` also take a `CompressOption` as their second argument. When the second argument is omitted on any of these four methods, the same settings as `CompressOptions.Default` are used.

The settings are not contained in the compressed data. `CompressOption` is serializable.

| Setting | Purpose |
| --- | --- |
| `FrequentSequences` | Up to 6 frequently occurring strings, given as a `string[]`. Unused trailing elements may be omitted. `null` and empty strings raise an `ArgumentException`. Non-ASCII text, such as Japanese, may be used. |
| `Templates` | Up to 5 fixed patterns such as dates and times. Constants such as `Templates.IsoDate` may be used. |
| `HCodes` | The horizontal code and bit length for each of the 5 character groups. Use `null` for a group that is not used. |

Every setting on a `CompressOption` you create can be changed. For example, custom frequent sequences are given like this.

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

When a setting that restricts the character classes of the input, such as `AlphaOnly`, is given a character it cannot represent, a `UnishoxFormatException` is thrown.

## Compatibility and incompatibilities with siara-cc/Unishox2

A UUID appearing in the middle of a string is also compressed with the dedicated format. Cross-decompression with siara-cc/Unishox2 has been verified.

### An implementation difference in lines self-references

For data that uses a lines self-reference whose source, at restore time, extends into a range that has not been written yet, Sakilabo.Unishox2 restores it by sequential copying.

Data containing a self-reference affected by this defect behaves as follows.

| Compressed by | Restored by `siara-cc/Unishox2` | Restored by Sakilabo.Unishox2 |
| --- | --- | --- |
| `siara-cc/Unishox2` | Cannot be restored correctly | Can be restored |
| Sakilabo.Unishox2 | Cannot be restored correctly | Can be restored |

The compression format is unchanged. The table above gives the concrete comparison result.

This concerns the `siara-cc/Unishox2` C implementation referenced by this repository. The affected code and what was verified are described in [the comparison with the C implementation](https://github.com/sakilabo/unishox2-dotnet/blob/main/tests/upstream/c-implementation-comparison.md).

### NUL characters in lines (incompatible)

NUL is treated as ordinary data, and the reference handling during lines compression and decompression covers the whole element. Because `siara-cc/Unishox2` uses `strlen()` for that handling, data containing a reference past a NUL may not be restorable by that implementation.

NUL is also handled in ordinary compression, without the lines feature.

## Building and the normal tests

**What you need: the .NET 10 SDK. No C compiler is required, and the siara-cc/Unishox2 submodule does not need to be fetched.**

Run all the commands below at the repository root, where this README lives.

```sh
dotnet build Sakilabo.Unishox2.sln -c Release
dotnet test tests/Sakilabo.Unishox2.Tests/Sakilabo.Unishox2.Tests.csproj -c Release
```

The normal tests verify compression and decompression in C#, the options, and reading and writing compressed data. The comparison tests against siara-cc/Unishox2 are skipped, and no C code is built or run.

In VS Code, open this folder and use the `build`, `test` and `pack` tasks. `Ctrl+Shift+B` runs `build`. CI also builds the C# code and runs the normal tests.

The library targets `netstandard2.0`, and the test project targets `net10.0`.

## Compatibility tests against siara-cc/Unishox2

**These are additional tests, run only when you want to verify cross-decompression with siara-cc/Unishox2. Running them requires a C compiler.**

### Prerequisites

In addition to the .NET 10 SDK used for normal development, you need Git and the following C build environment.

| OS | C build environment required |
| --- | --- |
| Windows | Visual Studio 2022 or the Build Tools with "Desktop development with C++", including the MSVC x64 tools and the Windows SDK. |
| Linux | `gcc` and the development headers of the C standard library. |

On Windows the tests locate Visual Studio with `vswhere.exe` and set up the build environment through `VsDevCmd.bat`. They can be run from an ordinary PowerShell session or the VS Code terminal.

### Procedure

At the repository root, first fetch the siara-cc/Unishox2 sources to compare against.

```sh
git submodule update --init --recursive
```

Then enable the comparison tests and run them.

```sh
dotnet test tests/Sakilabo.Unishox2.Tests/Sakilabo.Unishox2.Tests.csproj -c Release -p:RequireNativeHarness=true
```

In addition to the normal tests, this command does the following.

1. Builds `tests/upstream/Unishox2/unishox2.c` and `tests/upstream/harness/harness.c` automatically, with MSVC on Windows and GCC on Linux.
2. Starts the C executable it produced.
3. Verifies that data compressed in C# decompresses in C, and that data compressed in C decompresses in C#.

You do not need to build the C sources by hand. The tests fail if the compiler or the siara-cc/Unishox2 sources cannot be found, or if the build or the cross-decompression fails. They do not judge exact equality of the compressed bytes, nor which implementation compresses more tightly. For the [incompatible areas](#compatibility-and-incompatibilities-with-siara-ccunishox2), they verify that Sakilabo.Unishox2 can restore the original text.

The C executable exists solely for the comparison tests. It is not part of the library or the NuGet package.

For the build settings per compiler and the details of the comparison, see [the comparison with the C implementation](https://github.com/sakilabo/unishox2-dotnet/blob/main/tests/upstream/c-implementation-comparison.md).

## Building the NuGet package

Run this at the repository root. No C compiler is required.

```sh
dotnet pack src/Sakilabo.Unishox2/Sakilabo.Unishox2.csproj -c Release -o artifacts
```

This produces the `.nupkg` and the `.snupkg` symbol package in `artifacts/`.

For how to add it to a project that uses it, see [Installation](#installation).

## Updating the siara-cc/Unishox2 code

The siara-cc/Unishox2 code used for comparison is managed as a git submodule at `tests/upstream/Unishox2`.

1. Fetch the siara-cc/Unishox2 sources.

   ```sh
   git submodule update --init --recursive
   ```

2. Regenerate the official test cases with Python 3 and copy them into the test project.

   ```sh
   python tests/upstream/extract_testcases.py
   python -c "from shutil import copyfile; copyfile('tests/upstream/testcases.json', 'tests/Sakilabo.Unishox2.Tests/UpstreamTestCases.json')"
   ```

3. Run the [compatibility tests against siara-cc/Unishox2](#compatibility-tests-against-siara-ccunishox2).
4. Update the results recorded in this README and in [the comparison with the C implementation](https://github.com/sakilabo/unishox2-dotnet/blob/main/tests/upstream/c-implementation-comparison.md).

## License

- The compression and decompression logic and the data tables based on siara-cc/Unishox2: [Apache License 2.0](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE-APACHE.txt).
- The newly written C# API and wrapper code: [UPL 1.0](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE-UPL.txt). Copyright 2026 株式会社さきラボ.

For the applicable licenses and the full copyright notices, including those of siara-cc/Unishox2, see [LICENSE](https://github.com/sakilabo/unishox2-dotnet/blob/main/LICENSE).
