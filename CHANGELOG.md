# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/QuinntyneBrown/GetFiles/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/QuinntyneBrown/GetFiles/releases/tag/v1.0.0
