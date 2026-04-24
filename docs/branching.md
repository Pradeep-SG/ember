# Branching model

Simple on purpose. Two people, one codebase, iOS-first — anything more ceremonious than this is a tax.

## Branches

- **`main`** — always buildable, always reflects what would ship if we froze today. Protected: no direct pushes in Phase 1+.
- **`feature/<topic>`** — one branch per unit of work. Short-lived. Branches from `main`, merges back into `main`.

No `develop`, no `release/*`, no `hotfix/*`. Phase 0 and Phase 1 do not need them.

## Naming

- Lowercase, hyphen-separated.
- Prefix with `feature/`. Examples: `feature/shadow-battle-phase-1`, `feature/oath-onboarding`, `feature/resonance-meter`.
- For pure doc or infra changes: `feature/docs-pillars`, `feature/ci-cache-fix`. We don't split `docs/*` or `chore/*` out — it fractures tooling for no gain.

## Merging

- **Squash-merge into `main`.** One branch = one commit on `main`. The squash commit is the unit of rollback.
- **No rebase on shared branches.** Rebase locally on your own feature branch before opening a PR; never rewrite history on a branch someone else may have pulled.
- **Pull request required** once there is more than one committer (i.e., from Phase 1 onward — during Phase 0 solo doc commits direct to `main` are acceptable).
- Delete the branch after merge. `gh pr merge --squash --delete-branch` is the default.

## Commit messages

- Imperative mood, present tense: "Add breathing cadence spec", not "Added" or "Adds".
- Scope prefix optional, only when it genuinely clarifies: `shadow-battle: tune tap tolerance`.
- Body explains the *why* when non-obvious. The *what* is in the diff.

## When this changes

When a third person starts committing, or when we start cutting releases against TestFlight, revisit. Until then, leave it alone.
