# Pull Request Procedure

This document is the step-by-step guide for preparing, opening, and merging a pull request in SaaFarr.AI.Prompt. Follow it in order. Repository settings and Actions details live in [GITHUB.md](GITHUB.md). Contribution rules live in [CONTRIBUTING.md](CONTRIBUTING.md).

There is no separate `.nuspec` file. The NuGet manifest is generated at pack time from `SaaFarr.AI.Prompt.csproj` (`PackageId`, `Version`, `PackageReleaseNotes`, README, LICENSE). Version and package notes are updated in that project file.

## Contents

- [Pull Request Procedure](#pull-request-procedure)
  - [Contents](#contents)
  - [1. When CI runs](#1-when-ci-runs)
  - [2. Branch naming](#2-branch-naming)
  - [3. Start from an up-to-date main](#3-start-from-an-up-to-date-main)
  - [4. Create the working branch](#4-create-the-working-branch)
  - [5. Make the change](#5-make-the-change)
  - [6. Changelog](#6-changelog)
  - [7. Version and package metadata](#7-version-and-package-metadata)
  - [8. Verify locally](#8-verify-locally)
  - [9. Commit and push](#9-commit-and-push)
  - [10. Open the pull request](#10-open-the-pull-request)
  - [11. Wait for CI](#11-wait-for-ci)
  - [12. Review and merge](#12-review-and-merge)
  - [13. Publish a NuGet version](#13-publish-a-nuget-version)
  - [Quick checklist](#quick-checklist)

---

## 1. When CI runs

`.github/workflows/ci.yml` does **not** run on every branch you push. It runs when:

- You **push** to `main`, `master`, or a branch whose name matches `v*` (for example `v1.0`), or
- You open or update a **pull request whose base** is `main`, `master`, or `v*`.

A branch named `feature/yaml-output` does not run CI by itself. CI runs after you open a pull request **into `main`** (or into a release branch such as `v1.0`).

Do not name a feature or fix branch `v1.2.3` or `v-anything`. The `v*` pattern is reserved for release stabilization branches. A mistaken `v*` name would run CI on every push as if it were a release line, and it would not match the intended pull-request workflow.

The **Release** workflow (nuget.org) does not run on pull requests. It runs when a maintainer pushes a tag matching `v*.*.*` (for example `v1.1.0`) or starts **Actions**, **Release**, **Run workflow**. See section 13 and [GITHUB.md](GITHUB.md) section 6.

---

## 2. Branch naming

Use a prefix, a slash, and a short kebab-case description. ASCII letters, digits, and hyphens only.

- `feature/` — new behavior or public API
- `fix/` — defect
- `docs/` — documentation only
- `chore/` — dependencies, lock files, tooling
- `ci/` — GitHub Actions or coverage wiring
- `release/` — version bump and changelog freeze before a tag (example: `release/1.1.0`)

Examples: `feature/yaml-output-format`, `fix/message-template-render`, `docs/github-guide`, `chore/nuget-lock-files`, `ci/setup-dotnet-cache`, `release/1.1.0`.

Target of the pull request:

- Default: **`main`**
- Hotfix for a shipped line: the matching release branch, for example **`v1.0`**

---

## 3. Start from an up-to-date main

Do not branch from a stale local `main`. Fetch remotes first, check out `main`, and fast-forward it to `origin/main`. Then create the working branch.

If you have a fork, `origin` is your fork and `upstream` is `johnsonmima/SaaFarr.AI.Prompt`. Maintainers with write access often have `origin` pointing at `johnsonmima/SaaFarr.AI.Prompt`.

Maintainer (write access):

```bash
git fetch origin
git checkout main
git pull --ff-only origin main
```

Contributor (fork). Add `upstream` once if it is missing:

```bash
git remote add upstream https://github.com/johnsonmima/SaaFarr.AI.Prompt.git
git fetch upstream
git checkout main
git merge --ff-only upstream/main
git push origin main
```

`git pull --ff-only` (or `merge --ff-only`) fails if local `main` has commits that are not on the remote. Do not mix that work into `main`. Move those commits onto a feature branch, reset local `main` to the remote, and continue.

Confirm you are on the latest `main` before creating the branch:

```bash
git status
git log -1 --oneline
```

`git status` should report a clean working tree on `main`, in sync with `origin/main` or `upstream/main`.

---

## 4. Create the working branch

From the updated `main`:

```bash
git checkout -b feature/short-description
```

Replace the prefix and description using section 2. Do not create the branch, then later rebase onto `main` only if you skipped section 3. Starting from current `main` avoids that.

If `main` moved after you started work, update the branch before you open or refresh the pull request:

```bash
git fetch origin
git rebase origin/main
```

On a fork, rebase onto `upstream/main`. Resolve conflicts, then force-push only that feature branch (`git push --force-with-lease`). Never rebase or force-push `main`.

---

## 5. Make the change

Keep the pull request to one concern. Do not mix a public API change, a CI rewrite, and a dependency bump in the same request.

While editing:

- New public members need XML documentation.
- New or changed behavior needs tests under `tests/SaaFarr.AI.Prompt.Tests/`. Prefer cases that match README or [AGENT.md](AGENT.md) in `EndToEndUsageTests` or `AgentTests` when those files are the right place.
- Namespaces and package id stay `SaaFarr.AI.Prompt`.
- Do not commit secrets, `.env` files, or personal paths.
- If you change a `PackageReference`, run `dotnet restore SaaFarr.AI.Prompt.sln` and commit both `packages.lock.json` and `tests/SaaFarr.AI.Prompt.Tests/packages.lock.json`. CI restore uses `--locked-mode` and will fail if the lock files are stale.

---

## 6. Changelog

For any user-facing behavior or API change, add an entry under `## [Unreleased]` in `CHANGELOG.md`. Use **Added**, **Changed**, **Fixed**, or **Removed**. Do not create a new version heading on an ordinary feature or fix pull request.

Documentation-only and internal CI changes may skip the changelog unless maintainers ask for a note.

A **release** pull request (section 7 and 13) moves `[Unreleased]` items into a new heading such as `## [1.1.0] - YYYY-MM-DD` and leaves `[Unreleased]` empty for the next cycle.

---

## 7. Version and package metadata

SDK-style packing reads `SaaFarr.AI.Prompt.csproj`. There is no hand-maintained `.nuspec`. When you change version or release notes, edit these properties together:

- `<Version>` — NuGet package version (SemVer). This is what nuget.org and `dotnet add package` see.
- `<AssemblyVersion>` — four-part assembly version, for example `1.1.0.0`.
- `<FileVersion>` — four-part file version, usually the same as `AssemblyVersion`.
- `<PackageReleaseNotes>` — short text shipped inside the nupkg. Summarize the version you are publishing, not the entire Unreleased list.

SemVer for this library:

- **MAJOR** (`2.0.0`) — breaking public API. Mark the pull request as a breaking change.
- **MINOR** (`1.1.0`) — additive API or features, backward compatible.
- **PATCH** (`1.0.1`) — fixes and non-breaking corrections.

Do **not** bump `<Version>` on every feature or fix pull request. Development continues at the current package version with notes under `[Unreleased]`. Bump version only on a dedicated `release/x.y.z` pull request (or as the last change before the tag), when you intend to publish that version to nuget.org.

On a release pull request:

1. Choose the next SemVer from the Unreleased notes.
2. Set `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` in `SaaFarr.AI.Prompt.csproj`.
3. Set `<PackageReleaseNotes>` to a concise summary of that version (drawn from CHANGELOG).
4. Move `[Unreleased]` entries to `## [x.y.z] - YYYY-MM-DD` in `CHANGELOG.md`.
5. Keep `<PackageId>` as `SaaFarr.AI.Prompt`. Do not rename the package in a routine PR.

CI **Pack** uses whatever `<Version>` is in the csproj on that commit. It uploads a `.nupkg` artifact for inspection. It does not push to nuget.org.

---

## 8. Verify locally

From the repository root:

```bash
dotnet restore SaaFarr.AI.Prompt.sln --locked-mode
dotnet build SaaFarr.AI.Prompt.sln -c Release --no-restore
dotnet test SaaFarr.AI.Prompt.sln -c Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include=[SaaFarr.AI.Prompt]* /p:Threshold=95 /p:ThresholdType=line /p:ThresholdStat=total
```

If you changed package metadata and want to inspect the nupkg:

```bash
dotnet pack SaaFarr.AI.Prompt.csproj -c Release --no-restore -o ./artifacts /p:ContinuousIntegrationBuild=true
```

CI will fail the pull request if tests fail, if line coverage of `SaaFarr.AI.Prompt` is below 95 percent, if restore is not locked, or if `dotnet pack` fails.

---

## 9. Commit and push

Stage only the files that belong to this change. Do not add `TEMP.md`, `CODE_REVIEW.md`, `bin/`, `obj/`, or coverage output (those are gitignored).

Push the **working branch**, not `main`:

```bash
git push -u origin HEAD
```

On a fork, `origin` is your fork. Maintainers push the feature branch to `johnsonmima/SaaFarr.AI.Prompt`.

---

## 10. Open the pull request

Open the pull request against **`main`** (or the `v*` release branch for a hotfix). That base branch is what makes CI run.

Fill `.github/PULL_REQUEST_TEMPLATE.md`:

- Summary: what changed and why.
- Type of change.
- Test plan, including local `dotnet test`.
- Checklist: package id, changelog, XML docs, no secrets.

Title the pull request as a short imperative sentence, for example `Add YAML output format` or `Fix MessageTemplate extra placeholders`. GitHub squash-merge will use a similar title as the commit on `main`.

If GitHub shows that the branch is behind `main`, use **Update branch** on the pull request (or rebase as in section 4) before asking for review.

---

## 11. Wait for CI

On the pull request, **Checks** (or **Actions**) must show **Build**, **Test**, and **Pack** green. Those names are required by branch protection. Do not merge while they are pending or red.

A first-time contributor’s workflow may sit on **Waiting for approval**. A maintainer must open the pull request **Actions** tab and approve the run. See [GITHUB.md](GITHUB.md).

If **Build** fails: compile error or locked restore. If **Test** fails: failing tests or coverage below 95 percent. If **Pack** fails: missing README/LICENSE in the pack, bad csproj metadata, or lock-file mismatch.

Fix the branch, push, and wait for a new run. Do not close and reopen a pull request only to retry CI unless the workflow never started.

---

## 12. Review and merge

Required: at least one approving review, conversation resolution, up to date with the base branch, and the three CI checks.

Public API or `.github/workflows/` changes should have a second review when a second maintainer is available.

Merge with **Squash and merge** only. Merge commits and rebase-merge are disabled. After squash, GitHub can delete the head branch.

Do not merge your own pull request if you are the only reviewer and protection requires another approval. Do not bypass required checks.

---

## 13. Publish a NuGet version

Publishing is not part of an ordinary pull request. After a **release** pull request (section 7) is squash-merged to `main`:

1. Confirm `main` has the intended `<Version>` (for example `1.1.0`).
2. Tag that commit. The tag must match `v*.*.*` and should match `<Version>`:

```bash
git checkout main
git pull --ff-only origin main
git tag -a v1.1.0 -m "v1.1.0"
git push origin v1.1.0
```

3. Pushing the tag starts `.github/workflows/release.yml`, which builds, tests, packs, and pushes to nuget.org using Trusted Publishing. Prerequisites (nuget.org policy and `NUGET_USER`) are in [GITHUB.md](GITHUB.md) section 6.
4. To rehearse without publishing, use **Actions**, **Release**, **Run workflow**, with `dry_run` set to `true`.

Do not run `dotnet nuget push` from a laptop. Do not put a nuget.org API key in the pull request or in the repository.

---

## Quick checklist

Copy this into your notes while working. Details are in the sections above.

1. `git fetch` and update local `main` with `--ff-only`.
2. Branch from that `main` using `feature/`, `fix/`, `docs/`, `chore/`, `ci/`, or `release/` (never a stray `v*` name).
3. Implement one concern. Tests and XML docs as required. Refresh lock files if packages changed.
4. Changelog under `[Unreleased]` when user-facing. Bump csproj `<Version>` / `<AssemblyVersion>` / `<FileVersion>` / `<PackageReleaseNotes>` only on a release PR.
5. Restore `--locked-mode`, Release build, tests with 95 percent line coverage.
6. Push the working branch. Open a PR **into `main`** (or `v*`). Fill the template.
7. Wait for **Build**, **Test**, and **Pack**. Address review.
8. Squash-merge. For a nuget.org release, merge a version bump first, then push tag `vX.Y.Z`.
