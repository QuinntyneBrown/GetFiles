# GetFiles

A .NET CLI tool that aggregates repository source code into a single file for LLM consumption.

## Installation

```bash
dotnet tool install --global QuinntyneBrown.GetFiles
```

## Usage

```bash
get-files aggregate --path <repository-path> [options]
```

### Options

| Option | Description | Default |
|---|---|---|
| `--path` | Root path of the repository to scan | *(required)* |
| `--output` | Output file path | `codebase.txt` |
| `--strip-comments` | Remove comments from source files | `false` |
| `--strip-whitespace` | Collapse unnecessary whitespace | `false` |
| `--ignore` | Additional folders/paths to exclude (repeatable) | *(none)* |

### Examples

Aggregate a repository with default settings:

```bash
get-files aggregate --path ./my-repo
```

Strip comments and whitespace to reduce token count:

```bash
get-files aggregate --path ./my-repo --output output.txt --strip-comments --strip-whitespace
```

Exclude additional directories:

```bash
get-files aggregate --path ./my-repo --ignore coverage .vscode
```

## Supported File Types

`.ts`, `.html`, `.scss`, `.css`, `.cs`, `.csproj`, `.sln`, `.json`, `.yaml`, `.yml`

## How It Works

1. **File Discovery** - Recursively walks the repository tree, filtering by supported extensions and respecting `.gitignore` rules. Directories like `node_modules`, `dist`, `bin`, `obj`, and `.git` are always excluded.
2. **Code Aggregation** - Reads each discovered file and writes it to a single output file with clear `=== FILE: path ===` delimiters.
3. **Optional Processing** - Comments (C-style `//`, `/* */`, and HTML `<!-- -->`) and excess whitespace can be stripped to minimize token usage.

## Building from Source

```bash
cd backend
dotnet build
```

## Local Install

To build, uninstall any existing version, and install the latest locally:

```cmd
backend\eng\scripts\install-tool.bat
```

This packs the tool in Release configuration, removes any previously installed global version, and installs the freshly built package.

## Running Tests

```bash
cd backend
dotnet test
```

## License

MIT
