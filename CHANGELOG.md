# Changelog

## [0.1.0] - 2026-09-15

First public release.

### Added

- Compresses a `string` or a UTF-8 `byte[]` with Unishox2 and restores it to a `string`.
- Compresses an array of strings as lines, using repetitions within an element and across earlier elements.
- 17 predefined settings on `CompressOptions`, including `Default`, `Json`, `Url`, `Xml` and `Html`, with customisable frequent sequences, templates and horizontal codes.
- Targets .NET Standard 2.0 with no native dependencies; using the library needs no C compiler.
- Detects a UUID anywhere in the string, not only at the start, by comparing the position relative to the start of the UUID.
- Treats NUL as ordinary data in the lines feature, covering the whole element in the reference search.
- Restores an overlapping lines self-reference by copying one byte at a time.
- Throws `UnishoxFormatException` for invalid UTF-8, unpaired surrogates, and characters a restricted setting cannot represent.
- Verified against the 159 official test cases of siara-cc/Unishox2, including cross-decompression with its C implementation.
