<div align="center">

# GetFiles

**Aggregate an entire repository into a single, LLM-ready text file.**

A fast, dependency-light .NET global tool that walks a repository, honors your `.gitignore`,
and concatenates the source into one clean file — optionally stripping comments and whitespace
to keep your token budget lean.

[![CI](https://github.com/QuinntyneBrown/GetFiles/actions/workflows/ci.yml/badge.svg)](https://github.com/QuinntyneBrown/GetFiles/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/QuinntyneBrown.GetFiles.svg?logo=nuget)](https://www.nuget.org/packages/QuinntyneBrown.GetFiles)
[![NuGet Downloads](https://img.shields.io/nuget/dt/QuinntyneBrown.GetFiles.svg?logo=nuget)](https://www.nuget.org/packages/QuinntyneBrown.GetFiles)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

</div>

---

## Table of Contents

- [Why GetFiles?](#why-getfiles)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage](#usage)
  - [Options](#options)
  - [Examples](#examples)
- [Output Format](#output-format)
- [Supported File Types](#supported-file-types)
- [How It Works](#how-it-works)
- [Building from Source](#building-from-source)
- [Running Tests](#running-tests)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [Security](#security)
- [License](#license)

## Why GetFiles?

Large language models reason best when they can see a whole codebase at once, but copying files
by hand is tedious and wastes context on boilerplate. **GetFiles** automates that work:

- 🗂️ **One file, whole repo** — recursively collects supported source files into a single output.
- 🚫 **Respects `.gitignore`** — honors root and nested ignore rules, and always skips
  `node_modules`, `dist`, `bin`, `obj`, and `.git`.
- ✂️ **Token-aware** — strips comments and collapses whitespace by default to shrink the payload
  you hand to an LLM (toggle off when you want the originals).
- 🧭 **Deterministic output** — files are emitted in a stable, sorted order with clear delimiters.
- ⚡ **Zero ceremony** — install once as a .NET global tool and run it anywhere.

## Installation

GetFiles is distributed as a [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
on [NuGet](https://www.nuget.org/packages/QuinntyneBrown.GetFiles). You'll need the
[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer.

```bash
dotnet tool install --global QuinntyneBrown.GetFiles
```

Update to the latest release:

```bash
dotnet tool update --global QuinntyneBrown.GetFiles
```

Uninstall:

```bash
dotnet tool uninstall --global QuinntyneBrown.GetFiles
```

## Quick Start

```bash
gf aggregate --path ./my-repo
```

This scans `./my-repo` and writes everything to `codebase.txt` in the current directory, with
comments and excess whitespace stripped. That's it — paste the result into your LLM of choice.

## Usage

```bash
gf aggregate --path <repository-path> [options]
```

`aggregate` is the default command, so it can be omitted — `gf -p <repository-path>`
is equivalent to `gf aggregate -p <repository-path>`.

### Options

| Option | Alias | Description | Default |
| --- | --- | --- | --- |
| `--path` | `-p` | Root path of the repository to scan. | _(required)_ |
| `--output` | `-o` | Output file path. | `codebase.txt` |
| `--strip-comments` | | Remove comments from source files. | `true` |
| `--no-strip-comments` | | Keep comments intact (overrides `--strip-comments`). | `false` |
| `--strip-whitespace` | | Collapse unnecessary whitespace. | `true` |
| `--no-strip-whitespace` | | Keep whitespace intact (overrides `--strip-whitespace`). | `false` |
| `--ignore` | `-i` | Additional folder/path names to exclude. Repeatable. | _(none)_ |

> **Note**
> Comment and whitespace stripping are **on by default** to minimize token usage. Pass
> `--no-strip-comments` and/or `--no-strip-whitespace` to preserve the source verbatim.

### Examples

Aggregate a repository with the default settings (comments and whitespace stripped):

```bash
gf aggregate --path ./my-repo
```

Preserve the source exactly as written:

```bash
gf aggregate --path ./my-repo --no-strip-comments --no-strip-whitespace
```

Write to a custom output file:

```bash
gf aggregate --path ./my-repo --output context.txt
```

Exclude additional directories (the flag is repeatable):

```bash
gf aggregate --path ./my-repo --ignore coverage --ignore .vscode
```

The `--path`, `--output`, and `--ignore` options also accept short aliases (`-p`, `-o`, `-i`),
and the `aggregate` command name can be dropped entirely:

```bash
gf -p ./my-repo -o context.txt -i coverage
```

## Output Format

Each file is wrapped in clearly labeled delimiters so both humans and models can tell where one
file ends and the next begins. Paths are repository-relative and use forward slashes on every
platform.

```text
=== FILE: src/index.ts ===
export const greet = (name: string) => `Hello, ${name}!`;
=== END FILE: src/index.ts ===

=== FILE: src/styles.css ===
body { margin: 0; }
=== END FILE: src/styles.css ===
```

When aggregation finishes, GetFiles reports the number of files processed, the total line count,
and the output file size.

## Supported File Types

`.ts` · `.html` · `.scss` · `.css` · `.cs` · `.csproj` · `.sln` · `.json` · `.yaml` · `.yml`

Files with any other extension are skipped during discovery.

## How It Works

1. **File discovery** — recursively walks the repository, including only
   [supported extensions](#supported-file-types). It applies `.gitignore` rules from both the root
   and nested ignore files, and always excludes `node_modules`, `dist`, `bin`, `obj`, and `.git`.
   The resulting list is sorted for deterministic output.
2. **Aggregation** — reads each discovered file and writes it to the output with
   `=== FILE: path ===` / `=== END FILE: path ===` delimiters and a blank line between entries.
3. **Optional processing** — by default, comments (C-style `//` and `/* */`, plus HTML
   `<!-- -->`) and excess whitespace are stripped to reduce token count. The comment stripper uses
   a character-by-character state machine so comment-like sequences inside string and template
   literals are preserved.

## Building from Source

The solution lives under [`backend/`](backend) and targets .NET 8.

```bash
cd backend
dotnet build
```

To pack, uninstall any existing global version, and install your freshly built tool in one step
(Windows):

```cmd
backend\eng\scripts\install-tool.bat
```

## Running Tests

The project is covered by an [xUnit](https://xunit.net/) test suite.

```bash
cd backend
dotnet test
```

## Project Structure

```text
.
├── backend/
│   ├── GetFiles.sln
│   ├── eng/scripts/install-tool.bat   # Pack + reinstall the global tool locally
│   ├── src/GetFiles/                  # CLI entry point, commands, and services
│   └── tests/GetFiles.Tests/          # xUnit test suite
├── docs/specs/                        # Design specifications
├── .github/workflows/                 # CI and release pipelines
└── README.md
```

## Contributing

Contributions are welcome and appreciated! Please read our [Contributing Guide](CONTRIBUTING.md)
to get started, and note that this project adheres to a [Code of Conduct](CODE_OF_CONDUCT.md). A
list of everyone who has helped shape GetFiles lives in [CONTRIBUTORS.md](CONTRIBUTORS.md).

## Security

If you discover a security vulnerability, please follow the process described in our
[Security Policy](SECURITY.md). Please do **not** open a public issue for security reports.

## License

GetFiles is released under the [MIT License](LICENSE).
