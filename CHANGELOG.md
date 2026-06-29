# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- The `aggregate` command now always ignores `package-lock.json` files at any
  depth, regardless of `.gitignore` content. These generated lock files are
  large and add only noise to the aggregated output.

## [1.3.0]

### Added

- `aggregate` is now the default command and can be omitted: `gf -p ./repo` is
  equivalent to `gf aggregate -p ./repo`. Help (`-h`, `--help`, `-?`) and version
  (`--version`) flags are passed through unchanged, and the explicit
  `gf aggregate ...` form continues to work.

## [1.2.0]

### Added

- Short aliases for the `aggregate` options: `-p` (`--path`), `-o` (`--output`),
  and `-i` (`--ignore`).

## [1.1.0]

### Changed

- **Breaking:** renamed the CLI command from `get-files` to `gf` for quicker
  invocation. The package ID (`QuinntyneBrown.GetFiles`) is unchanged, so
  installation is still `dotnet tool install --global QuinntyneBrown.GetFiles`;
  update any scripts that invoked `get-files` to use `gf`.

### Added

- Continuous integration workflow (build & test on Linux, Windows, and macOS).
- Release workflow to pack and publish the tool to NuGet on tagged releases.
- Project documentation: `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`,
  `CONTRIBUTORS.md`, `LICENSE`, and issue/pull request templates.

## [1.0.0]

### Added

- Initial release of the `get-files` .NET global tool.
- `aggregate` command that walks a repository and concatenates supported source files into a single
  output file for LLM consumption.
- `.gitignore`-aware file discovery (root and nested), with `node_modules`, `dist`, `bin`, `obj`,
  and `.git` always excluded.
- Comment stripping for C-style and HTML files via a string-literal-aware state machine.
- Whitespace stripping to reduce token usage.
- `--ignore` option to exclude additional directories.

[Unreleased]: https://github.com/QuinntyneBrown/GetFiles/compare/v1.3.0...HEAD
[1.3.0]: https://github.com/QuinntyneBrown/GetFiles/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/QuinntyneBrown/GetFiles/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/QuinntyneBrown/GetFiles/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/QuinntyneBrown/GetFiles/releases/tag/v1.0.0
