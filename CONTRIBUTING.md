# Contributing to Mima.AI.Prompt

Thanks for helping improve this library. This document explains how to contribute effectively to an open-source .NET package.

## Why this file exists

Open-source projects receive contributions from people who have never met the maintainers. Clear contribution rules reduce friction, keep PR review focused, and protect release quality (tests, API stability, license compatibility).

## Code of Conduct

Participation is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Ways to contribute

- Report bugs and request features via [GitHub Issues](https://github.com/johnsonmima/Mima.AI.Prompt/issues)
- Improve documentation (README, [AGENT.md](AGENT.md), XML docs, samples)
- Add or fix unit tests (prefer README/`AGENT.md`-aligned cases in `EndToEndUsageTests` / `AgentTests`)
- Propose API improvements via an Issue **before** a large PR

## Development setup

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) that supports `net10.0` (SDK 10+ recommended; multi-targeting also builds `netstandard2.0`, `net6.0`, `net8.0`).
2. Clone the repository:

```bash
git clone https://github.com/johnsonmima/Mima.AI.Prompt.git
cd Mima.AI.Prompt
```

3. Restore and build:

```bash
dotnet restore Mima.AI.Prompt.sln
dotnet build Mima.AI.Prompt.sln -c Release
```

4. Run tests with coverage:

```bash
dotnet test Mima.AI.Prompt.sln -c Release --no-restore \
  '/p:CollectCoverage=true' \
  '/p:CoverletOutputFormat=cobertura' \
  '/p:Include=[Mima.AI.Prompt]*' \
  '/p:Threshold=95' \
  '/p:ThresholdType=line' \
  '/p:ThresholdStat=total'
```

Quote `/p:Include=...` in zsh. Do not pass `--no-build` when collecting coverage; Coverlet instruments at compile time.

## Branching & pull requests

The full procedure (update `main`, branch names, CI triggers, version and package metadata, merge, tag) is in [PR.md](PR.md). Short form:

1. Fork the repository (or create a branch if you have write access).
2. Update local `main` from the remote, then create a feature branch:

```bash
git checkout -b feature/short-description
```

3. Keep changes focused. Prefer small PRs over multi-concern mega-PRs.
4. Ensure all tests pass and new public APIs have tests.
5. Update `CHANGELOG.md` under `[Unreleased]` when behavior or API surface changes.
6. Open a Pull Request against `main` (or the designated release branch). Fill in the PR template.

### PR requirements

- [ ] Builds on all target frameworks
- [ ] Unit tests added/updated; suite green
- [ ] XML documentation on new public members
- [ ] No secrets, credentials, or personal paths committed
- [ ] `CHANGELOG.md` updated when user-facing
- [ ] Follows existing naming: `Mima.AI.Prompt.*` namespaces
- [ ] If you change `PackageReference` versions, run `dotnet restore` and commit the updated `packages.lock.json` files (CI restore uses `--locked-mode`)

## Coding standards

- Prefer immutable types for domain objects (messages, prompts).
- Keep the core library provider-agnostic; HTTP clients belong in adapter packages.
- Avoid breaking public API without a major version bump (SemVer).
- Use PolySharp-friendly modern C#; do not introduce APIs that break `netstandard2.0` without a polyfill or `#if`.

## Security

Do not open public issues for undisclosed vulnerabilities. See [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
