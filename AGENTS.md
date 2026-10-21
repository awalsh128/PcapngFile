# AGENTS.md

Guidance for AI coding agents working in this repository. Humans should start with [README.md](README.md).

## Project

PcapngFile is a .NET 10 library that reads PCAP Next Generation (pcapng) capture files into CLR objects. It's published to NuGet as [PcapngFile](https://www.nuget.org/packages/PcapngFile/) (owner `awalsh128`).

The spec is the IETF draft [draft-ietf-opsawg-pcapng](https://datatracker.ietf.org/doc/draft-ietf-opsawg-pcapng/). Check behavior against it, not against other readers or older drafts: the old winpcap.org spec and some earlier drafts differ (e.g. flag bit numbering).

## Layout

| Path | Contents |
| --- | --- |
| `PcapngFile/` | The library. One class per block type, plus `Reader`, `BlockBase`, `EndianBinaryReader`, `TimestampTransformer`, enums. |
| `PcapngFile/PcapngFile.nuget.props` | The NuGet package definition: ID, **version**, description, release notes, license, readme, Source Link. Imported by `PcapngFile.csproj`. |
| `PcapngFile.Tests/` | NUnit tests. `TestFile1.pcapng` and `TestFile2.pcapng` are embedded resources. |
| `PcapngFile.slnx` | Solution file (XML format; there is no `.sln`). |
| `.github/workflows/ci.yml` | Build and test on push and pull requests to `master`, then update the `latest` GitHub release. |
| `.github/workflows/publish.yml` | Manually dispatched NuGet publish (see Publishing). |
| `scripts/convert-logo.sh` | Converts `logo.svg` to a NuGet-compatible PNG icon. |
| `nuget.config` | Restore sources only (nuget.org). It does not define the package. |

## Build and test

```bash
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
```

The build must stay at **zero warnings**. `GenerateDocumentationFile` is on, so any public or protected member without an XML doc comment produces CS1591.

If the .NET 10 SDK isn't installed, use the SDK container. Run it as the current user and send build output to the container's own temp folder so no root-owned `bin/`/`obj/` folders end up in the repo:

```bash
docker run --rm --user "$(id -u):$(id -g)" -e HOME=/tmp -v "$PWD":/src:ro -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  sh -c "dotnet build -c Release --artifacts-path /tmp/a && dotnet test --no-build -c Release --artifacts-path /tmp/a"
```

Packing: `GeneratePackageOnBuild` is on, so build first and then run `dotnet pack PcapngFile/PcapngFile.csproj --no-build -c Release`. Running `dotnet pack` without `--no-build` fails with NU5026.

## Architecture

- **`Reader`** reads one whole block at a time, in order, into a byte array, and never seeks. That's what lets it read unseekable streams. Peeking (`PeekStoreType`, `PeekClrType`) keeps the next block in a `pending` field. `Reset()` and the automatic reset after an enumeration need a seekable stream.
- **Byte order is per section.** Each Section Header Block's byte-order magic (`0x1A2B3C4D`) sets the byte order of the blocks after it. A file can mix little- and big-endian sections.
- **Per-section state:** `Reader.TrackSection` also skips whole sections whose major version isn't 1, and remembers the snap length of the section's first interface, which `SimplePacketBlock` needs to drop padding. It runs on every raw block, including ones an iterator filters out.
- **`EndianBinaryReader`** is an internal `BinaryReader` subclass over one block's bytes that reverses byte order for big-endian sections. Block constructors take a `BinaryReader` and read fields with it as usual.
- **Option values** are raw `byte[]`. Never decode them with `BitConverter` or `Encoding.UTF8` directly. Use the `BlockBase` helpers:
  - `ToByte`, `ToInt32`, `ToUInt32`, `ToInt64`, `ToUInt64` and `ToTimestamp`, which follow the section's byte order and check the length
  - `ToUtf8String`, which stops at a zero byte
  - `ToPacketHash`, and `RequireLength`/`RequireMinimumLength` for byte arrays
- **Invalid lengths:** the helpers throw a private exception when an option has the wrong length. `ReadOptions` catches it, ignores the option and records its code in `InvalidOptionCodes`, as the spec allows. New option handlers must validate lengths the same way.
- **Options:** `BlockBase.TryReadOptions` reads until `opt_endofopt` or the end of the block, since the end marker is optional. It handles `opt_comment` and custom options (2988/2989/19372/19373) itself and passes every other option to the block's `OnReadOptionsCode` override, which must ignore codes it doesn't know.
- **Repeatable options** (comments, `if_IPv4addr`, `if_IPv6addr`, `epb_hash`, `epb_verdict`, `epb_processid_threadid`, `pack_hash`) are kept in lists exposed as `IReadOnlyList<T>`. The older singular property (e.g. `Comment`, `Hash`) returns the first one.
- **Block layout:** `ReadClosingField` reads the trailing length at the end of the block, not the current position, because blocks can have unused space after their options.
- **Unsupported types are skipped:** block types (Custom, Decryption Secrets, private types) and name resolution record types the library doesn't support are skipped, as the spec requires, not thrown on.
- **Timestamps** are unsigned 64-bit values stored as high 32 bits then low 32 bits. `TimestampTransformer` converts them using `if_tsresol` (base 10 or base 2; default 6 = microseconds) and `if_tsoffset`, with 128-bit math. It returns UTC and throws `ArgumentOutOfRangeException` outside the `DateTime` range. Blocks expose raw `long` timestamps plus `GetTimestamp(InterfaceDescriptionBlock, out bool)` helpers, which use `BlockBase.ToDateTime`.
- **Construction:** block constructors are `internal`, so no one outside the assembly can subclass `BlockBase`. New internal-only helpers there should be `private protected`, so they don't need public XML docs.
- **Tests:** `AllowUnitTesting.cs` gives `PcapngFile.Tests` access to internals.

## Conventions

- **Match the surrounding code.** The C# files use block-scoped namespaces with `using` directives inside them, `this.` qualification, 4-space indentation, and `System` usings first in their own group. `.editorconfig` suggests some styles the code doesn't follow (e.g. file-scoped namespaces); don't mass-reformat.
- **New library `.cs` files** need the BSD 2-Clause license header (`Copyright (c) 2013-2026, Andrew Walsh`) and UTF-8 with BOM, like the existing ones.
- **Preserve each file's encoding and line endings** when editing. They vary: `ReaderTests.cs` has CRLF, most files have LF, and library files have a BOM. A plain read and write in a script can silently convert CRLF to LF, so use `newline=''` in Python, for example.
- **XML docs:** write a doc comment for every public or protected member. Name the spec field an option property comes from (e.g. `if_tsresol`) and say when the property can be `null`.
- **Tests:** NUnit 4 with `Assert.That(actual, Is...)` constraints; the classic `Assert.AreEqual` style isn't available. Run byte-order-sensitive tests for both byte orders (`[TestCase(false)] [TestCase(true)]`). `BlockOptionsTests` has a `SectionWriter` that builds pcapng sections byte by byte in either byte order; extend it rather than adding binary fixtures.
- **Release notes:** when you change behavior, add a line to `<PackageReleaseNotes>` in `PcapngFile.nuget.props`, and update README.md if usage changes.
- **Commits:** short, sentence-case messages ending with a period (e.g. `Add badges for CI status and NuGet version.`). Reference fixed issues with `Fixes #N`.

## Spec compliance

For a broader check, run the library over [hadrielk/pcapng-test-generator](https://github.com/hadrielk/pcapng-test-generator). It has 24 little-endian and 24 big-endian test files, each with a `.txt` file listing its expected blocks. All 48 currently read correctly; the only differences are the Custom Blocks, which are skipped on purpose.

Every block and option defined for the standard blocks is read. What's left is optional:
- Custom Blocks and Decryption Secrets Blocks are skipped rather than exposed as block classes.
- `if_filter` is kept as raw bytes, because the spec doesn't define the filter formats yet.
- Invalid UTF-8 in strings is replaced with U+FFFD. The spec allows this.

Open GitHub issues: https://github.com/awalsh128/PcapngFile/issues

## Publishing

- **Version:** set `<Version>` in `PcapngFile/PcapngFile.nuget.props`. nuget.org rejects a version that already exists, so bump it for every release.
- **Workflow:** `publish.yml` runs only when dispatched manually. It has a `dry-run` input that packs without pushing.
- **Authentication:** NuGet Trusted Publishing. `NuGet/login@v1` swaps a GitHub OIDC token for a short-lived API key, so no password or API key secret is stored. It needs a trusted publishing policy on nuget.org for `awalsh128/PcapngFile`, workflow `publish.yml`, environment `nuget`.
- **Environment:** the job runs in the GitHub environment `nuget`. The VS Code Actions extension flags it until that environment exists in the repo settings.
- **Package contents:** the DLL, XML docs, README and a `.snupkg` symbol package with Source Link.

## Things to avoid

- Don't add a `.sln` next to `PcapngFile.slnx`: with two solution files, plain `dotnet build`/`test` can't choose one and fails.
- Don't open files for writing in library code. `Reader(string)` uses `File.OpenRead` so read-only files work.
- Don't reintroduce `BinaryReader.PeekChar()` for end-of-file detection. It decodes UTF-8 and stops early on blocks whose first byte is 0.
- Don't commit build output (`bin/`, `obj/`, `artifacts/`) or editor folders such as `.vscode/`.
