# Source Priority

## Priority Order

Use these sources in this order when reconstructing the intended workflow for a repo:

1. Current user instructions and confirmed decisions in the active conversation
   Start with what the user has explicitly confirmed in the current task.
2. Decision records
   Read the relevant `accepted` or `proposed` files in `./docs/decisions/`.
3. Repo instructions
   Read `./AGENTS.md` if present.
4. Supporting repo artifacts
   Read `./spec/`, `./spec/testcases/`, `./docs/`, `./tests/`, and source code.

## What Each Source Is Authoritative For

- Current user instructions:
  What has been explicitly requested, confirmed, deferred, or ruled out in the current task.
- Decision records:
  Accepted architecture and naming baselines, plus what was intentionally excluded.
- `AGENTS.md`:
  Phase gate, file placement, wording, commit format, and delivery expectations.
- `spec` and `spec/testcases`:
  Formal baseline after Phase 1 freeze.
- Source and tests:
  Actual current behavior and delivery completeness.

## Resolve Conflicts

- If current user instructions conflict with repo docs, prefer the newer confirmed user decision and record the update in `./docs/decisions/` if it materially changes the baseline.
- If `AGENTS.md` conflicts with an `accepted` decision, call out the mismatch explicitly and ask whether Phase 1 is being reopened.
- If `./docs/project-roadmap.md` is missing, say so directly and fall back to accepted decisions plus `./AGENTS.md`.

## Use This Rule in Practice

- For a new architecture direction, read current user instructions and decisions first.
- For implementation under frozen spec, read `spec` and tests first, then source.
- For historical commit analysis, read the exact snapshot first and only use current repo docs as organizational guidance.
