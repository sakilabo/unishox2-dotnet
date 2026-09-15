# tests/upstream

A Japanese version of this document is available as [c-implementation-comparison.ja.md](./c-implementation-comparison.ja.md).

This folder holds the external material (siara-cc/Unishox2) and the verification tooling used solely for testing and verifying Sakilabo.Unishox2. The notes here are Sakilabo.Unishox2's own verification material, not part of siara-cc/Unishox2 itself.

- `Unishox2/` — a git submodule of [siara-cc/Unishox2](https://github.com/siara-cc/Unishox2). After the first clone, fetch it with `git submodule update --init --recursive`. It is licensed under the Apache License 2.0, with the copyright notice `Copyright (C) 2020 Siara Logics (cc)`. Sakilabo.Unishox2 does not modify its source code.
- `harness/harness.c` — a test CLI written for Sakilabo.Unishox2 that only calls `unishox2.c` and `unishox2.h` from siara-cc/Unishox2, without modifying them. It implements a one-request-per-line protocol over standard input and output; the list of commands is in the comment at the top of the file. Copyright 2026 株式会社さきラボ, UPL-1.0.
- `extract_testcases.py` and `testcases.json` — a tool that extracts the test case strings from `run_unit_tests()` in `test_unishox2.c` of siara-cc/Unishox2, and its output: 159 entries, excluding the single entry that contains a NUL byte. `tests/Sakilabo.Unishox2.Tests/UpstreamTestCases.json` is a copy of that output placed in the test project.

The comparison against siara-cc/Unishox2 runs only when requested explicitly with `dotnet test -p:RequireNativeHarness=true`. The harness and the siara-cc/Unishox2 code are built with MSVC on Windows and gcc on Linux, and cross-decompression is verified.

The normal test run uses C# only, on every operating system. It needs neither a C compiler nor the siara-cc/Unishox2 submodule. Without the comparison flag the 16 relevant tests are skipped and no compiler is detected or invoked. When the comparison is requested and the build or run fails, that is treated as a test failure carrying diagnostic information.

For the procedure and the prerequisites, see [the compatibility tests section of the README](../../README.md#compatibility-tests-against-siara-ccunishox2).

## NUL bytes in the lines feature (incompatible)

Sakilabo.Unishox2 does not treat NUL as a terminator: the reference handling during compression and decompression covers the whole element. On the compression side `CompressLineChain.GetLength` returns the array length, and on the decompression side `DecompressLineChain.GetLength` returns either the current output length or the array length of an earlier element.

In siara-cc/Unishox2, `matchLine()` and `decodeRepeat()` both use `strlen()`, for the current element (ctx=0) as well as for earlier elements. Compressed data from Sakilabo.Unishox2 that contains a reference past a NUL may therefore fail to decompress with siara-cc/Unishox2. This is an accepted incompatibility, and the compression format itself is unchanged. See [the incompatibility section of the README](../../README.md#nul-characters-in-lines-incompatible).

`NativeLinesNulByteTests.cs` verifies that data containing NUL bytes compressed by siara-cc/Unishox2 can be decompressed in C#. It does not require siara-cc/Unishox2 to decompress C# data containing a reference past a NUL, and a failure on the siara-cc/Unishox2 side is not a pass condition either. The C# reference search, self-references, references to earlier elements, and restore-after-save are all covered by the ordinary managed tests.

## Implementation differences from siara-cc/Unishox2

The differences observed in the comparison tests, and how Sakilabo.Unishox2 handles each.

### 1. How a lines self-reference is copied

- Result for compression and restoration: Sakilabo.Unishox2 can decompress the affected data whether it was compressed by siara-cc/Unishox2 or by C#.
- Where: `decodeRepeat()` in `unishox2.c`, namely `memmove(out + ol, cur_line->data + dist, min_of(left, dict_len))`. For a ctx=0 self-reference, `cur_line->data` is the same buffer as `out`, so this is effectively `memmove(out + ol, out + dist, min_of(left, dict_len))`.
- How to reproduce: `dist` is the absolute position from the start of the element, the copy source start index, rather than a backward distance from the current position. The problem occurs when an element contains a short-period repetition such as `"XAXAXAXA..."` and, under the lines feature, the compressor's `matchLine()` emits a ctx=0 self-reference whose source range overlaps output that has not been written yet, that is, an overlapping copy with `dist + dict_len > ol`, where `ol` is the decoder's current output position.
- Impact: when decompressed in the realistic pattern where the decoder does not retain the source text of the element being decompressed, which is the usual case of storing only the byte sequence and decompressing later, part of that element's output is lost. `memmove()` is specified to use the source values as they were at the time of the call, so an overlapping copy that reads not-yet-written memory cannot act as a sequential self-referential copy in the LZ77 sense.
- What is verified: the automated tests confirm that data compressed by siara-cc/Unishox2 decompresses in C#, and that the C# round-trip holds.
- How Sakilabo.Unishox2 handles it: `DecodeRepeat()` and `DecodeOccurance()` in `Internal/LineMatching.cs` copy one byte at a time while reading the value just written, so the same bit sequence as siara-cc/Unishox2 decompresses correctly. This departs from upstream rather than following it. Confirmed by `LinesAndSelfReferenceTests.SelfReferencingOverlapWithinSingleElement_RoundTrips`.

### 2. A difference in where a UUID (GUID) is detected

- Where: the inner UUID detection loop in the main compression loop of `unishox2.c`, namely `if (c_uid == '-' && (uid_pos == 8 || uid_pos == 13 || uid_pos == 18 || uid_pos == 23))`.
- How to reproduce: a UUID pattern that appears somewhere other than the start of the string being compressed, that is, at l>0. `uid_pos` is an absolute index into the whole string, and the comparison should have been against `uid_pos - l`, the position relative to the start of the UUID, rather than against the absolute position.
- Impact: a UUID at l>0 does not use the dedicated compression, the dense 4-bit-per-nibble encoding, and falls back to ordinary HEX detection or literal encoding. The round-trip still holds and only the compression ratio drops slightly.
- Whether the decoder is affected: the decoding side of `unishox2.c`, where the nibble sequence of the UUID encoding is read, never consults the position (l) after the escape code and always reads a fixed 32 nibbles. Choosing the UUID encoding is a compressor-side decision only, and the structure of the emitted bit sequence is identical for l==0 and l>0. Correcting the compressor to use the relative position therefore does not affect compatibility with any decoder of the emitted bit sequence, whether the unmodified siara-cc/Unishox2 C implementation or Sakilabo.Unishox2. The only thing that changes is the compressed bytes themselves, through which encoding gets chosen.
- How Sakilabo.Unishox2 handles it: it compares against `uid_pos - l`. For input containing a UUID at l>0 the compressed bytes may differ, but `NativeUuidCompressionTests.cs` confirms that both implementations can decompress each other's output.

## Known caveats in the verification tooling itself (harness.c and NativeHarness.cs)

Everything above concerns the behaviour of `unishox2.c` in siara-cc/Unishox2 itself, which is kept distinct from the following. Any constraints or known caveats in the verification tooling, `harness.c` and `NativeHarness.cs`, are recorded in this section as they are found. There is no known issue to record at present.

## Build settings for the C harness

- Windows: `vswhere.exe` locates a Visual Studio installation that has `Microsoft.VisualStudio.Component.VC.Tools.x86.x64`, then `VsDevCmd.bat -arch=x64 -host_arch=x64` is run and the build uses `cl /TC /std:c11 /utf-8 /W3`.
- Linux: the build uses `gcc -DUNISHOX_API_WITH_OUTPUT_LEN=1 -O2`.
- On both platforms, `harness.c` and `unishox2.c` from siara-cc/Unishox2 are compiled with the siara-cc/Unishox2 folder on the include path. The siara-cc/Unishox2 sources themselves are not modified.
- MSVC support in the harness consists of a macro replacing `strtok_r` with `strtok_s`, and `_CRT_SECURE_NO_WARNINGS` to suppress the deprecation warning for `sscanf`.
- The API with an output length is not enabled under MSVC. `#if (UNISHOX_API_OUT_AND_LEN(0,1)) == 0` in siara-cc/Unishox2 expands to an expression containing a comma, which produces C1012, so the default API without an output length is used instead. Interoperability of the compression and decompression results has been verified with the MSVC build.

The startup logic itself is implemented in [NativeHarness.cs](../Sakilabo.Unishox2.Tests/NativeHarness.cs).
