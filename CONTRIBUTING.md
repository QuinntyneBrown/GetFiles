# Contributing to GetFiles

First off — thank you for taking the time to contribute! 🎉

GetFiles is an open source project and we welcome contributions of all kinds: bug reports, feature
requests, documentation improvements, and code. This guide explains how to get involved and what to
expect.

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Table of Contents

- [Ways to Contribute](#ways-to-contribute)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Enhancements](#suggesting-enhancements)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Coding Guidelines](#coding-guidelines)
- [Commit Messages](#commit-messages)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [License](#license)

## Ways to Contribute

- 🐛 **Report a bug** — open an issue with a clear, minimal reproduction.
- 💡 **Suggest a feature** — describe the problem you're trying to solve, not just the solution.
- 📝 **Improve docs** — typos, clarifications, and examples are all valuable.
- 🔧 **Submit code** — fix a bug or implement an enhancement via a pull request.

## Reporting Bugs

Before opening a new issue, please search [existing issues](https://github.com/QuinntyneBrown/GetFiles/issues)
to avoid duplicates. A great bug report includes:

- The command you ran (including all flags).
- What you expected to happen and what actually happened.
- Your operating system and the output of `dotnet --version`.
- The installed tool version (`get-files --version`).

## Suggesting Enhancements

Open an issue describing the use case and the problem it solves. If you have an idea of how the API
or output should look, include a short example — but focus on the "why" so the maintainers can help
shape the "how."

## Development Setup

You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer.

```bash
git clone https://github.com/QuinntyneBrown/GetFiles.git
cd GetFiles/backend
dotnet restore
dotnet build
dotnet test
```

To try your local build as a global tool, run the helper script (Windows), which packs, uninstalls
any existing version, and installs the fresh build:

```cmd
backend\eng\scripts\install-tool.bat
```

## Making Changes

1. **Fork** the repository and create a branch from `main`:
   ```bash
   git checkout -b feature/short-description
   ```
2. Make your change in small, focused commits.
3. **Add or update tests** for any behavior you change. The suite uses
   [xUnit](https://xunit.net/) and lives in `backend/tests/GetFiles.Tests`.
4. Make sure everything builds and all tests pass:
   ```bash
   cd backend
   dotnet build
   dotnet test
   ```
5. Update documentation (including this repo's `README.md`) when you change user-facing behavior.

## Coding Guidelines

- Target **.NET 8** with nullable reference types and implicit usings enabled (matching the
  existing projects).
- Follow standard .NET naming conventions and keep the existing file/folder layout
  (`Commands/`, `Services/`, one interface per service).
- Prefer **constructor dependency injection** — services are registered in `Program.cs` and
  resolved through `Microsoft.Extensions.DependencyInjection`.
- Keep public types and methods documented with XML doc comments, consistent with the current code.
- Write code that reads like the surrounding code: match its naming, structure, and comment density.

## Commit Messages

Write clear, imperative commit messages ("Add nested gitignore support", not "Added"/"Adds").
[Conventional Commits](https://www.conventionalcommits.org/) are encouraged but not required:

```text
feat: add --max-file-size option to skip large files
fix: preserve comment-like sequences inside template literals
docs: clarify default stripping behavior in README
```

## Submitting a Pull Request

1. Push your branch and open a pull request against `main`.
2. Fill out the pull request template, linking any related issues (e.g. `Closes #123`).
3. Ensure CI is green — pull requests must build and pass tests on Linux, Windows, and macOS.
4. A maintainer will review your change. Please be responsive to feedback; small follow-up commits
   are fine and we squash on merge where appropriate.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE) that covers this project.
