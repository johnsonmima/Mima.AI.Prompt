# GitHub Repository Guide

This document describes how the public GitHub repository for Mima.AI.Prompt is configured and operated. It is the maintainer and contributor reference for pull requests, GitHub Actions, branch protection, and publishing the NuGet package.

Repository: [https://github.com/johnsonmima/Mima.AI.Prompt](https://github.com/johnsonmima/Mima.AI.Prompt)

Package identifier: `Mima.AI.Prompt`

Workflow files: `.github/workflows/ci.yml` and `.github/workflows/release.yml`

## Audience and permissions

Maintainer procedures in this guide require the **Admin** role on the repository. Organization policies can override repository settings. If a control is disabled, review the organization’s **Settings**, then **Actions**.

Contributor sections explain what happens on a fork pull request and which checks must pass before merge. They do not require Admin access.

Paths in this guide start from the repository home page. Repository configuration lives under the **Settings** tab.

## Contents

- [GitHub Repository Guide](#github-repository-guide)
  - [Audience and permissions](#audience-and-permissions)
  - [Contents](#contents)
  - [1. Setup order](#1-setup-order)
  - [2. Repository profile](#2-repository-profile)
  - [3. Default branch](#3-default-branch)
  - [4. Contribution model](#4-contribution-model)
  - [5. GitHub Actions](#5-github-actions)
    - [5.1 Enable Actions](#51-enable-actions)
    - [5.2 Verify that CI runs](#52-verify-that-ci-runs)
    - [5.3 Continuous integration jobs](#53-continuous-integration-jobs)
      - [Build](#build)
      - [Test](#test)
      - [Pack](#pack)
    - [5.4 Release workflow](#54-release-workflow)
  - [6. NuGet Trusted Publishing](#6-nuget-trusted-publishing)
    - [6.1 Register a policy on nuget.org](#61-register-a-policy-on-nugetorg)
    - [6.2 Store the nuget.org username on GitHub](#62-store-the-nugetorg-username-on-github)
    - [6.3 Verify a release](#63-verify-a-release)
    - [6.4 Optional GitHub Environment](#64-optional-github-environment)
    - [6.5 Why not a long-lived API key](#65-why-not-a-long-lived-api-key)
  - [7. Pull request settings](#7-pull-request-settings)
  - [8. Branch protection](#8-branch-protection)
    - [8.1 Rulesets](#81-rulesets)
    - [8.2 Classic branch protection](#82-classic-branch-protection)
  - [9. Code review](#9-code-review)
  - [10. Security and Dependabot](#10-security-and-dependabot)
  - [11. Issues](#11-issues)
  - [12. Packages](#12-packages)
  - [13. Roles](#13-roles)
  - [14. Maintainer setup procedure](#14-maintainer-setup-procedure)
  - [15. Troubleshooting](#15-troubleshooting)
    - [CI does not start](#ci-does-not-start)
    - [Pull requests cannot merge](#pull-requests-cannot-merge)
    - [Release does not publish](#release-does-not-publish)
  - [16. References](#16-references)

---

## 1. Setup order

GitHub cannot require a status check until that check has been produced by at least one workflow run. Configure the repository in this order:

1. Set the repository profile and default branch to `main`.
2. Enable GitHub Actions (section 5.1).
3. Confirm that `.github/workflows/ci.yml` and `.github/workflows/release.yml` exist on `main`.
4. Run the **CI** workflow once (a push to `main` or a pull request) and wait until the **Build**, **Test**, and **Pack** jobs succeed.
5. Protect `main` and require those three checks.
6. Configure squash merging, the NuGet secret, security features, and Dependabot.

If status checks are required before step 4, the checks dropdown is empty and pull requests cannot be merged.

---

## 2. Repository profile

Navigate to **Settings**, then **General**.

Set the repository name to `Mima.AI.Prompt` so it matches the NuGet package identifier and assembly name.

Set the description to a short summary suitable for GitHub and nuget.org discovery, for example: strongly typed fluent prompt engineering library for .NET.

Set the website to `https://github.com/johnsonmima/Mima.AI.Prompt` so it matches `PackageProjectUrl` in the project file.

Add topics that aid search, such as `dotnet`, `csharp`, `llm`, `prompt-engineering`, `openai`, `anthropic`, and `nuget`.

Keep visibility **Public**. Public repositories on GitHub.com receive complimentary GitHub-hosted Actions minutes for standard workflows.

Under **Features**, enable **Issues**. Leave **Discussions** optional. Disable **Wikis**; project documentation lives in `README.md` and `AGENT.md`. **Projects** and **Sponsorships** are optional.

Pull request merge options on the same page are described in section 7.

---

## 3. Default branch

Navigate to **Settings**, then **General**, and locate **Default branch**. The same control may appear under **Settings**, then **Branches**.

The default branch must be `main`. The CI workflow runs on pushes and pull requests targeting `main`, `master`, and branches matching `v*`.

Optional release branches such as `v1.0` may be used for stabilization. Protect them with the same rules as `main`.

If the default branch is still a release line such as `v1.0`, switch it to `main` once `main` is the development trunk, or protect `v1.0` equivalently.

---

## 4. Contribution model

Public repositories on GitHub.com allow forks by default.

External contributors work by forking the repository, creating a branch, and opening a pull request into `main`. They do not receive push access to this repository.

Collaborators with write access still use pull requests after `main` is protected. Direct pushes to `main` should not be possible.

---

## 5. GitHub Actions

Two workflows are defined in the repository.

The **CI** workflow (`.github/workflows/ci.yml`) runs on push and pull request to `main`, `master`, and `v*` branches. It contains three jobs in sequence: **Build**, **Test**, and **Pack**.

The **Release** workflow (`.github/workflows/release.yml`) runs when a tag matching `v*.*.*` is pushed, or when a maintainer starts it from **Actions**, then **Release**, then **Run workflow**. It is the only workflow that may publish to nuget.org.

Both workflows use GitHub-hosted `ubuntu-latest` runners. Self-hosted runners are not used.

`actions/setup-dotnet@v6` is configured with `cache: true` so restored NuGet packages are stored in GitHub’s cache. Caching requires committed `packages.lock.json` files for the library and the test project, and restore commands use `--locked-mode`.

### 5.1 Enable Actions

Navigate to **Settings**, then **Actions**, then **General**.

Set **Actions permissions** to **Allow all actions and reusable workflows**. The workflows use `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`, and `NuGet/login`. Restricting the repository to local actions only will fail those steps.

The Test job posts a coverage comment on pull requests. That needs `pull-requests: write` on the job (already declared in `ci.yml`) and a token that can comment. Keep **Workflow permissions** as **Read and write permissions**, or keep the default read token and rely on the Test job’s explicit `permissions` block.

Disable **Allow GitHub Actions to create and approve pull requests**. Maintainers merge pull requests. Workflows must not approve their own changes.

Set **Fork pull request workflows from outside collaborators** to **Require approval for first-time contributors**. A first-time fork will wait until a maintainer selects **Approve and run** on that pull request’s Actions tab. This reduces the risk of a first contribution mining secrets or consuming minutes maliciously.

Set **Workflow permissions** to **Read and write permissions**. Uploading coverage and package artifacts requires write access. Read-only permissions will fail the upload steps unless each job declares explicit `permissions` in YAML.

Leave **Allow GitHub Actions to send GITHUB_TOKEN to workflows from forks** at the default that does not send secrets to fork workflows. Fork pull requests must not receive `NUGET_USER` or an OIDC token that can publish.

Save the page.

Navigate to **Settings**, then **Actions**, then **Runners**. Keep GitHub-hosted runners. Do not register a self-hosted runner unless the project intends to operate one.

### 5.2 Verify that CI runs

Open the **Actions** tab (not Settings). The **CI** workflow should appear in the list.

Open the latest run. The jobs must be named **Build**, **Test**, and **Pack**, matching `jobs.*.name` in `ci.yml`. Branch protection must require these names (GitHub may display them as `CI / Build` and similar). Do not require a check named `CI` unless a job with that exact name exists.

If no run appears, the workflow file is missing from the default branch, Actions are disabled, or the organization blocks Actions.

The README badge URL is `https://github.com/johnsonmima/Mima.AI.Prompt/actions/workflows/ci.yml/badge.svg`. It reports no status until at least one run exists.

On a fork pull request, open **Actions** or **Checks** on the pull request. First-time contributors remain waiting for approval until a maintainer approves the workflow.

### 5.3 Continuous integration jobs

The **CI** workflow runs **Build**, then **Test**, then **Pack**. Each job starts on a new `ubuntu-latest` virtual machine. The `needs` relationship only gates start order: Test does not start if Build failed, and Pack does not start if Test failed. Built outputs are not copied between jobs. That is why Test and Pack check out the repository, install the SDK, and restore packages again.

#### Build

**Build** is the first gate. It checks out the repository with `fetch-depth: 0` so SourceLink and version metadata can resolve full history. It installs .NET SDK 6.0.x, 8.0.x, and 10.0.x, restores `Mima.AI.Prompt.sln` with `--locked-mode`, and runs `dotnet build Mima.AI.Prompt.sln -c Release --no-restore`.

A green Build job means the library compiles for `netstandard2.0`, `net6.0`, `net8.0`, and `net10.0`.

#### Test

**Test** runs the xUnit suite with Coverlet and fails if line coverage of `Mima.AI.Prompt` is below 95 percent. Untested or under-covered changes must not merge.

The job is named `Test`, which is the label shown on Actions and on pull request checks. It declares `needs: build` so test minutes are not spent when the solution does not compile. It does not reuse Build binaries. It runs on `ubuntu-latest`.

**Checkout repository** uses `actions/checkout@v7` with the default fetch depth. Full git history is not required here. The step places the commit tree on disk so tests, sources, and lock files are available.

**Setup .NET SDK** uses `actions/setup-dotnet@v6` with SDK versions 6.0.x, 8.0.x, and 10.0.x. The test project targets `net10.0`, so 10.0.x is required to run tests. Versions 6 and 8 keep restore of the multi-targeted library consistent with Build.

`cache: true` stores the NuGet global-packages folder in GitHub’s cache after restore. Later runs with the same lock-file hash skip re-downloading packages such as PolySharp, xUnit, and Coverlet.

`cache-dependency-path` hashes both `packages.lock.json` and `tests/Mima.AI.Prompt.Tests/packages.lock.json`. If either file changes, GitHub uses a new cache key instead of restoring against a stale set.

`cache: true` requires those lock files in the repository. The setup-dotnet action fails if they are missing. Both project files set `RestorePackagesWithLockFile`. After changing a `PackageReference`, run `dotnet restore` locally and commit the updated lock files.

**Restore dependencies** runs `dotnet restore Mima.AI.Prompt.sln --locked-mode`. The solution is restored because tests reference the library. `--locked-mode` refuses to update lock files on the runner. If a contributor changes a package version without committing `packages.lock.json`, restore fails instead of resolving a different version.

**Run unit tests with coverage** executes `dotnet test` on `tests/Mima.AI.Prompt.Tests/Mima.AI.Prompt.Tests.csproj` with these arguments:

- `-c Release` uses the same configuration as Build and Pack.
- `--no-restore` skips a second restore.
- `/p:CollectCoverage=true` instruments the library with Coverlet.
- `/p:CoverletOutputFormat=cobertura` writes an XML report suitable for the upload step.
- `/p:CoverletOutput=./coverage/` writes under the test project directory, `tests/Mima.AI.Prompt.Tests/coverage/`, which is the upload path.
- `/p:Include=[Mima.AI.Prompt]*` measures this library only, not test frameworks.
- `/p:Threshold=95` fails the job below 95 percent coverage.
- `/p:ThresholdType=line` applies the threshold to line coverage.
- `/p:ThresholdStat=total` applies one total across the assembly.

If this step fails, tests failed, coverage is below 95 percent, or an earlier step failed. Fix tests or add coverage. Do not lower the threshold in a product change without a documented reason.

**Upload coverage report** uses `actions/upload-artifact@v4` with `if: always()` so the report is available even when tests fail. The artifact name is `coverage-report`. The path is `tests/Mima.AI.Prompt.Tests/coverage/`. `if-no-files-found: ignore` prevents a missing report from failing the job when Coverlet never ran. Retention is 14 days.

**Render coverage report** reads `coverage.cobertura.xml` and writes a markdown table (library totals plus per-file line and branch rates). It appends that table to the GitHub Actions job summary on the **Test** check. Open the Test job and expand **Summary** to read it.

**Comment coverage on pull request** posts or updates a sticky comment on the pull request with the same table. The Test job requests `pull-requests: write`. Fork pull requests may skip the comment if `GITHUB_TOKEN` cannot write to the parent repo; the job summary and coverage artifact still work. `continue-on-error: true` keeps a comment failure from failing Test after coverage already passed.

The Test job does not publish to nuget.org.

#### Pack

**Pack** runs `dotnet pack` on CI so packaging is verified on a clean runner, not only on a developer machine. It does not publish. Publishing is performed only by `.github/workflows/release.yml` (sections 5.4 and 6).

The job is named `Pack`. It is the third required check. A change that tests successfully but cannot pack (missing pack assets, invalid metadata, restore failure) must not merge.

It declares `needs: test`, so it runs only after Test (and therefore Build) succeeded. It still starts on a new virtual machine and repeats checkout, SDK setup, and restore. It runs on `ubuntu-latest`.

**Checkout repository** uses `actions/checkout@v7`. Packing needs `README.md`, `LICENSE`, the project file, and `packages.lock.json` from the same commit.

**Setup .NET SDK** uses `actions/setup-dotnet@v6` with 8.0.x and 10.0.x only. Packing does not execute the net6.0 test host. SDK 8 and 10 can pack all target frameworks in `Mima.AI.Prompt.csproj` (`netstandard2.0` through `net10.0`). Omitting 6.0.x reduces setup time.

`cache: true` uses the same NuGet cache mechanism as the other jobs. `cache-dependency-path` is `packages.lock.json` only, because this job restores the library project, not the test project.

**Restore dependencies** runs `dotnet restore Mima.AI.Prompt.csproj --locked-mode`. The library project is restored; tests are not packed. `--locked-mode` again requires the committed lock file to match `PackageReference` items.

**Pack NuGet package** runs `dotnet pack Mima.AI.Prompt.csproj` with `-c Release`, `--no-restore`, `-o ./artifacts`, and `/p:ContinuousIntegrationBuild=true`. Release is the configuration consumers receive. Output is written to `./artifacts` for the upload step. `ContinuousIntegrationBuild` enables deterministic, SourceLink-friendly packing (also implied by `env.CI` in the workflow).

On a successful run, open **Actions**, the run, **Pack**, then **Artifacts**. The package is for inspection. Continuous integration must not call `dotnet nuget push`. Fork pull requests must never receive a nuget.org key.

**Upload NuGet artifacts** uses `actions/upload-artifact@v4` with artifact name `nuget-packages`, path `./artifacts/*.nupkg`, and 14-day retention.

If Pack fails, typical causes are a lock-file mismatch, missing pack assets such as the README or LICENSE, or metadata errors from `dotnet pack`. Fix the project or lock files. Do not skip Pack on `main`.

### 5.4 Release workflow

The Release workflow is `.github/workflows/release.yml`. Start it from the repository **Actions** tab, workflow **Release**.

CI Pack only proves that `dotnet pack` succeeds. Release is the only workflow that may run `dotnet nuget push`.

The workflow starts when a git tag matching `v*.*.*` is pushed (for example `v1.0.0`), or when a maintainer uses **Run workflow** (`workflow_dispatch`).

The `dry_run` input applies to manual runs. The default `true` restores, builds, tests, packs, and uploads artifacts, and skips nuget.org. Use that value to rehearse a release. Setting `false` performs the same steps and then pushes to nuget.org.

A tag push always attempts the publish steps, because the condition is `github.event_name == 'push' || inputs.dry_run == 'false'`.

Those steps exchange a GitHub OIDC token for a one-hour nuget.org API key (section 6), then call `dotnet nuget push`. A dry run skips both. The CI workflow never publishes.

Do not store a long-lived nuget.org API key in the repository or in workflow YAML.

---

## 6. NuGet Trusted Publishing

This project follows Microsoft’s recommendation: [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing). GitHub Actions issues a short-lived OIDC token. nuget.org validates it against a policy you register, then returns a temporary API key (about one hour, one use). The workflow never stores a long-lived nuget.org API key.

The flow is:

1. The **Release** workflow runs on a tag or a non-dry `workflow_dispatch`.
2. GitHub issues an OIDC token because the job sets `permissions.id-token: write`.
3. `NuGet/login@v1` sends that token to nuget.org.
4. nuget.org checks the token against the Trusted Publishing policy and returns a temporary API key.
5. `dotnet nuget push` uses that key immediately. Requesting the key earlier in a long job can let it expire before the push.

Each OIDC token can be exchanged once. The login step therefore runs immediately before the push step, not at the start of the job.

The relevant steps in `release.yml` are:

```yaml
permissions:
  contents: read
  id-token: write

- name: NuGet login (OIDC)
  if: github.event_name == 'push' || inputs.dry_run == 'false'
  uses: NuGet/login@v1
  id: login
  with:
    user: ${{ secrets.NUGET_USER }}

- name: Push to NuGet.org
  if: github.event_name == 'push' || inputs.dry_run == 'false'
  env:
    NUGET_API_KEY: ${{ steps.login.outputs.NUGET_API_KEY }}
  run: >
    dotnet nuget push ./artifacts/*.nupkg
    --api-key "$NUGET_API_KEY"
    --source https://api.nuget.org/v3/index.json
    --skip-duplicate
```

`NUGET_USER` is the nuget.org **profile name**, not an email address and not an API key. Microsoft recommends storing it as a repository secret. Fork pull requests do not receive repository secrets. The Release workflow does not run on pull requests.

If **Trusted Publishing** is not listed on your nuget.org account, the feature may still be rolling out. Wait until it appears; do not add a long-lived API key to this workflow as a substitute.

### 6.1 Register a policy on nuget.org

1. Sign in at [https://www.nuget.org/](https://www.nuget.org/). Create an account if necessary.
2. Confirm that this account will own package identifier `Mima.AI.Prompt`. The first publish creates the package under the policy owner. Later publishes must use a policy owned by an owner of that package.
3. Select the username in the upper right, then **Trusted Publishing**.
4. Add a new policy. Values are case-insensitive. For this repository use:
   - **Repository Owner:** `johnsonmima`
   - **Repository:** `Mima.AI.Prompt`
   - **Workflow File:** `release.yml` (file name only; do not include `.github/workflows/`)
   - **Environment:** leave empty. The workflow does not set `environment:`. If you later add a GitHub Environment, put that same name here.
5. Choose policy ownership: yourself (individual) or an organization you belong to. The policy applies to all packages owned by that owner. If you leave an organization later, an org-owned policy can become inactive until you are added back.

A new policy on some repositories (especially private ones) may be temporarily active for seven days. It becomes permanently active after a successful publish, because nuget.org then records GitHub owner and repository IDs and can reject a deleted-and-recreated repo with the same name. If no publish occurs within seven days, the policy becomes inactive. You can restart the seven-day window from the nuget.org UI.

### 6.2 Store the nuget.org username on GitHub

Admin access on the repository is required.

1. Open the repository, then **Settings**.
2. In the sidebar, open **Secrets and variables**, then **Actions**.
3. Under **Repository secrets**, select **New repository secret**.
4. Set **Name** to `NUGET_USER`. The name must match `secrets.NUGET_USER` in `release.yml` exactly (case-sensitive).
5. Set **Secret** to the nuget.org profile name (the public username shown on nuget.org, not the Microsoft account email).
6. Select **Add secret**.

Do not store a nuget.org API key. `NuGet/login@v1` obtains a temporary key at publish time.

Do not add `NUGET_USER` to the CI workflow. CI runs on every pull request, including forks.

### 6.3 Verify a release

1. Open **Actions**, then **Release**, then **Run workflow**.
2. Leave `dry_run` set to `true`. Confirm restore, build, test, and pack succeed. This rehearsal does not call nuget.org.
3. To publish, push a tag such as `v1.0.0`, or run the workflow with `dry_run` set to `false`.
4. Open the run and inspect **NuGet login (OIDC)** then **Push to NuGet.org**. Failures usually mean the policy owner/repo/workflow file does not match, `NUGET_USER` is missing or is an email, `id-token: write` is missing, or Trusted Publishing is not enabled for the account.
5. After a successful push, the package appears at [https://www.nuget.org/packages/Mima.AI.Prompt](https://www.nuget.org/packages/Mima.AI.Prompt) after indexing (often a few minutes). `--skip-duplicate` makes a repeat push of the same version a no-op instead of a hard error.

### 6.4 Optional GitHub Environment

A GitHub Environment named `nuget` can be created under **Settings**, then **Environments**. Required reviewers on that environment, combined with `environment: nuget` on the Release job, force a human approval before push. The workflow does not set `environment` today. If you add it, set the same name on the nuget.org Trusted Publishing policy.

Repository secrets and OIDC tokens for this job are not available to fork pull requests. The Release workflow is not triggered by pull requests.

### 6.5 Why not a long-lived API key

A stored nuget.org API key is a password that can leak from logs, forks, or a compromised maintainer machine. nuget.org is also shortening API key lifetimes. Trusted Publishing avoids storing that secret: GitHub proves which repository and which workflow file requested the token, and nuget.org issues a key that expires in about an hour.

See [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

---

## 7. Pull request settings

Navigate to **Settings**, then **General**, and scroll to **Pull Requests**.

Disable **Allow merge commits** so `main` does not accumulate merge commits.

Enable **Allow squash merging** and use it as the default merge method. Each pull request becomes one commit on `main`. Intermediate work-in-progress commits remain visible on the pull request itself.

Disable **Allow rebase merging**. Casual open-source pull requests should not be rewritten on merge; rebase would keep noisy commits on `main`.

Enable **Automatically delete head branches** after squash so merged branches do not accumulate.

**Allow auto-merge** is optional. Enable it only if the project wants merge to proceed automatically when required checks are green.

Enable **Allow update branch** so contributors can update a pull request from the GitHub UI.

Enable **Suggest updating pull request branches** to reduce stale pull requests.

Squash merging keeps `main` as a linear history of completed changes. GitHub still shows the full commit list on the pull request.

The pull request template is `.github/PULL_REQUEST_TEMPLATE.md`. No additional UI toggle is required.

---

## 8. Branch protection

Apply protection only after the first successful CI run (section 1). Use either rulesets or classic branch protection, not both for conflicting rules on the same branch. Rulesets are the current GitHub interface.

### 8.1 Rulesets

Navigate to **Settings**, then **Rules**, then **Rulesets**, then **New ruleset**, then **New branch ruleset**.

Name the ruleset `main`. Set enforcement to **Active**. Do not grant bypass except for a documented break-glass administrator, so maintainers follow the same rules.

Target the branch `main`. Include `v*` as well if releases ship from version branches.

Enable **Restrict deletions** so `main` cannot be deleted.

Enable **Block force pushes**.

Enable **Require a pull request before merging** so there are no direct commits to `main`.

Require at least one approving review. Require two if there are two or more active maintainers.

Enable **Dismiss stale pull request approvals** so new commits need a new approval.

Enable **Require review from Code Owners**. `.github/CODEOWNERS` requests `@johnsonmima`.

Enable **Require conversation resolution** so review threads cannot be ignored.

Enable **Require status checks to pass**. Select **Build**, **Test**, and **Pack** from the dropdown after a green CI run. If GitHub lists them as `CI / Build`, `CI / Test`, and `CI / Pack`, select those names. Require all three. Pack depends on Test, which depends on Build.

Do not add a required check named `CI` unless a job with that name exists. Requiring a name that never runs prevents every pull request from merging.

Enable **Require branches to be up to date before merging**.

Enable **Require linear history**. This setting is compatible with squash merging only.

Leave **Require signed commits** off unless the project will enforce signing for all contributors. Signing blocks many first-time contributions.

Save the ruleset.

### 8.2 Classic branch protection

Alternatively, navigate to **Settings**, then **Branches**, then **Add branch protection rule**, with pattern `main`.

Apply the same policy: pull request required, at least one approval, dismiss stale approvals, code owners, conversation resolution, required checks **Build**, **Test**, and **Pack**, up to date with the base branch, linear history, include administrators, no force push, no deletions.

Enable **Include administrators** so maintainers cannot skip CI.

---

## 9. Code review

Require at least one approving review on every pull request so API and coverage regressions are caught before merge.

Require a second review for public API changes and for edits under `.github/workflows/`.

`.github/CODEOWNERS` covers `*` and `/.github/` and requests `@johnsonmima`.

---

## 10. Security and Dependabot

Navigate to **Settings**, then **Code security** (the page may be titled **Code security and analysis**).

Enable the dependency graph for supply-chain visibility.

Enable Dependabot alerts for vulnerable NuGet packages and GitHub Actions.

Enable Dependabot security updates so patch pull requests are opened automatically.

Enable Dependabot version updates. Schedule and ecosystems are defined in `.github/dependabot.yml` (NuGet and GitHub Actions, weekly on Monday).

Enable secret scanning.

Enable push protection so a push that contains a detected secret is blocked.

Code scanning (CodeQL) is recommended for additional static analysis.

Enable private vulnerability reporting so the GitHub **Report a vulnerability** control matches `SECURITY.md`.

Dependabot pull requests still require green **Build**, **Test**, and **Pack** checks and a human squash merge.

Point community guidelines or the code of conduct setting at `CODE_OF_CONDUCT.md` under **Settings**, **Moderation** or **General**.

---

## 11. Issues

Issue templates are files in `.github/ISSUE_TEMPLATE/`. There is no separate toggle beyond enabling Issues.

Navigate to **Settings**, then **General**, then **Features**, and enable **Issues**.

`.github/ISSUE_TEMPLATE/config.yml` disables blank issues and directs security reports to **Security**, then **Advisories**, as described in `SECURITY.md`.

---

## 12. Packages

The primary feed is nuget.org, not GitHub Packages.

CI **Pack** only uploads a `.nupkg` artifact on the Actions run (section 5.3). Publishing uses the Release workflow and Trusted Publishing (sections 5.4 and 6).

Enable `packages: write` in `release.yml` only if the project also pushes to GitHub Packages.

---

## 13. Roles

Anyone may clone or fork the repository and open issues.

A contributor working from a fork may open a pull request into `main`. The first workflow run from a new fork waits for maintainer approval.

Contributors cannot push to `main`, merge pull requests, publish to nuget.org, or change repository settings.

After branch protection is enabled, maintainers also cannot push directly to `main`. They merge pull requests after required checks and review.

Maintainers publish by pushing a `v*.*.*` tag or by running the Release workflow after the nuget.org Trusted Publishing policy and `NUGET_USER` secret are in place (section 6). Only Admins can change the settings in this guide.

---

## 14. Maintainer setup procedure

Complete these steps once when standing up or auditing the public repository.

1. Under **Settings**, **General**: public visibility, description, topics, Issues enabled, Wikis disabled.
2. Set the default branch to `main`.
3. Under **Settings**, **General**, **Pull Requests**: squash enabled; merge commits and rebase disabled; delete head branches enabled; update branch enabled.
4. Under **Settings**, **Actions**, **General**: allow all actions; require approval for first-time fork pull requests; workflow permissions read and write; save.
5. Confirm the **Actions** tab shows a successful **CI** run with **Build**, **Test**, and **Pack**.
6. Under **Settings**, **Rules** (or **Branches**): protect `main`; require those three checks; require linear history; block force pushes; include administrators.
7. Register a Trusted Publishing policy on nuget.org for `johnsonmima` / `Mima.AI.Prompt` / `release.yml`. Under **Settings**, **Secrets and variables**, **Actions**, add repository secret `NUGET_USER` (nuget.org profile name). Do not store an API key.
8. Under **Settings**, **Code security**: enable Dependabot, secret scanning, push protection, and private vulnerability reporting.
9. Confirm the README CI badge points at this repository’s `ci.yml`.
10. Confirm `LICENSE` is MIT at the repository root.

---

## 15. Troubleshooting

### CI does not start

Possible causes:

- Actions are disabled, or the organization policy is Disabled or Allow local actions only.
- The workflow file is not on the default branch.
- The `on.branches` list in the workflow does not include the default branch name.
- Actions are disabled for billing on a private fork. This repository is public.

### Pull requests cannot merge

Possible causes:

- The required check name does not match the job name. Re-run CI and select the name from the dropdown.
- A required check was configured that the workflow never produces.
- The branch is not up to date with `main`. Use **Update branch**.
- A first-time fork workflow is still waiting for approval.
- Linear history is required but merge commits are still allowed. Disable merge commits (section 7).

### Release does not publish

Possible causes:

- `dry_run` was left `true` on a manual run. Tag pushes always attempt publish.
- **Trusted Publishing** is not visible on the nuget.org account yet.
- The policy **Repository Owner**, **Repository**, or **Workflow File** does not match `johnsonmima`, `Mima.AI.Prompt`, and `release.yml`.
- `NUGET_USER` is missing, or it is an email address instead of the nuget.org profile name.
- The job lacks `permissions.id-token: write`.
- The policy is inactive (seven-day window expired without a successful publish, or the policy owner left the organization). Restart the window or restore membership on nuget.org.
- Actions are restricted to local actions only, so `NuGet/login@v1` cannot run.

---

## 16. References

- Repository: [https://github.com/johnsonmima/Mima.AI.Prompt](https://github.com/johnsonmima/Mima.AI.Prompt)
- NuGet package: [https://www.nuget.org/packages/Mima.AI.Prompt](https://www.nuget.org/packages/Mima.AI.Prompt)
- [Publish a NuGet package](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [Managing GitHub Actions settings](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/enabling-features-for-your-repository/managing-github-actions-settings-for-a-repository)
- [Available rules for rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets)
- [Protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches)
- [Using secrets in GitHub Actions](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
