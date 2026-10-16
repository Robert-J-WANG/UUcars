# Development and release workflow

This document describes the current Git and delivery workflow for UUcars. It is
a reference for maintainers; version design documents define scope rather than
repeating these procedures.

## Branch roles

| Branch | Responsibility |
|---|---|
| `main` | Production releases and milestone tags |
| `develop` | Integrated development baseline and release source |
| `feature/*` | Isolated feature or step implementation |
| `fix/*` | Isolated defect correction |
| `docs/*` | Documentation-only work |

## Normal development flow

Feature and fix work starts from the latest `develop`. The change is implemented
and validated on its own branch, then merged back with a merge commit. The branch
is retained until its release and required checks are complete.

```text
latest develop
→ feature or fix branch
→ implementation, tests and affected documentation
→ review
→ merge --no-ff into develop
→ verify develop CI
```

Daily integration into `develop` is a local repository workflow. It does not use
a pull request unless the owner intentionally changes that policy.

## Production release flow

Production releases use a pull request from `develop` to `main`. The release
source, backend, frontend and end-to-end checks must succeed before the pull
request is merged with a merge commit.

The successful `main` push identifies the exact commit used by the deployment
workflow. After the production release is verified, merge the resulting `main`
release commit back into `develop` so both long-lived branches share the same
history.

```text
verified develop
→ develop-to-main pull request
→ required CI checks
→ merge commit on main
→ deployment of that commit
→ production verification
→ synchronise main back into develop
```

Do not bypass failed checks, describe an unexecuted check as passing or treat a
configured deployment as verified before its real execution succeeds.

## Commit messages

Use a concise imperative summary with the conventional prefix that best matches
the change, such as `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `chore`,
`ci`, `merge` or `release`. A commit should describe one coherent change.

## Authority and worktree safety

Before any Git mutation, inspect the active branch and all staged, unstaged and
untracked files. Existing changes belong to the owner. Staging, committing,
pushing, merging, rebasing, deleting branches, rewriting history and creating
pull requests require explicit owner approval.
