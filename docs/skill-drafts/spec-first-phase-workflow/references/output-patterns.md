# Output Patterns

## Phase 1 Checklist

- Provide a short plan first.
- Produce `./docs/` material for review.
- Produce `./spec/`.
- Produce `./spec/testcases/`.
- Produce or update the repo's `.Abstract` contract project when that pattern exists.
- Make `./spec/` precise enough to map to code contract.
- Turn validate scenarios into executable tests, preferably unit tests.
- Give the stage its own contract and tests.
- Add class diagrams and C4 models for boundary review.
- Add sequence diagrams for scenario review.
- Add a decision table for coverage review.
- Keep the first pass thin and architecture-first.
- Stop for review/freeze before deep `.Core` or host refactor.
- Require build success and test success before review.

## Phase 2 Checklist

- Treat `./spec/` and `.Abstract` as frozen.
- Refactor `.Core` and host projects to match the frozen baseline.
- Validate against `spec/testcases` first.
- Add or update `Core.Tests` where useful.
- Keep implementation constrained by the frozen contract.
- Keep validate scenarios executable as tests.
- Keep class diagrams, C4 models, sequence diagrams, and decision table in sync with the stage.
- Keep working until review-ready unless the user redirects.
- Require build success and test success before review.

## Version Analysis Checklist

If the repo already has a phase-doc naming convention, reuse it.
Otherwise, for `./docs/phase-{n}-{commit}/`, create:

- `README.md`
- `c4-model.md`
- `testcases/README.md`
- `review-notes.md`
- testcase files with class diagram and sequence diagram

When applicable:

- Add `phase0 -> phase1` or `phase1 -> phase2` delta summary to `README.md`.
- Use exact commit snapshots as the source of truth.
- Do not rewrite older phases with newer canonical naming if the historical commit did not use it.
- Use class diagrams and C4 models to explain boundary shape.
- Use sequence diagrams to explain scenario flow.
- Use a decision table to show coverage shape and skipped space.

## Review Output Pattern

- Start with findings.
- Keep findings concrete and file-linked.
- Order findings by severity.
- Follow with open questions or assumptions.
- Keep the summary brief.

## Verification Pattern

- Use `dotnet build -m:1` and `dotnet test -m:1`.
- Run one msbuild-driven command at a time.
- State clearly when verification was partial, blocked, or environment-limited.
- Treat build as the contract gate.
- Treat tests as the scenario gate.
- Require both gates before asking for review.

## Commit Pattern

Use the repo's required commit structure:

1. First line: goal of the change
2. `milestones:`
3. `changes:`
4. `fixes:` when applicable
5. `comments:` when applicable
