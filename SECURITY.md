# Security Policy

## Why this file exists

Open-source packages are used in production systems. A clear security policy tells researchers and users **how** to report vulnerabilities privately, which versions are supported, and what to expect for response times — without dumping exploit details into public Issues.

## Supported versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a vulnerability

**Do not** file a public GitHub Issue for security vulnerabilities.

Please report privately via one of:

1. **GitHub Security Advisories** (preferred):  
   <https://github.com/johnsonmima/Mima.AI.Prompt/security/advisories/new>
2. Email the maintainer listed in the repository profile / NuGet package authors.

Include:

- Description of the issue and impact
- Steps to reproduce or proof-of-concept (kept private)
- Affected versions / target frameworks if known
- Suggested fix if you have one

## What to expect

- Acknowledgement within **7 days**
- Status update within **14 days**
- Coordinated disclosure after a fix is released when possible

## Scope notes

This library is a **prompt composition / validation** toolkit. It does not execute model inference or hold API keys by default. Security concerns typically involve:

- Unsafe deserialization of untrusted prompt JSON
- Injection via template variables rendered into prompts
- Supply-chain issues in published NuGet packages

Prompt *content* that causes an LLM to behave badly is generally out of scope unless it stems from a library bug (e.g., missing validation that the docs claim exists).
