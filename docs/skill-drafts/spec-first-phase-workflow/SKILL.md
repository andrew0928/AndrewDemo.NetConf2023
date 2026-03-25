---
name: spec-first-phase-workflow
description: Follow a spec-first, phase-gated software development workflow. Use when the user wants architecture-first planning, formal spec and testcase freeze before implementation, a separate `.Abstract` contract phase, decision logging, canonical naming consistency, or phase-by-phase commit analysis docs with C4, testcase diagrams, and review findings.
---

# Spec-First Phase Workflow

## Overview

Use this skill to keep software delivery aligned with a spec-first, phase-gated workflow where every stage is only review-ready after contract and validation both exist and both pass.

Review the work with this structure:

- Review boundaries and contract with class diagrams and C4 models.
- Review executable scenarios with sequence diagrams.
- Review coverage shape with a decision table that marks known scenarios, covered scenarios, and intentionally uncovered space.

Use this flow:

```text
User Request
    |
    v
Decide Stage Boundary
    |
    +--> Phase 1: shape boundary
    |       |
    |       +--> docs for review
    |       +--> spec precise to code contract
    |       +--> tests precise to validate scenarios
    |
    +--> Phase 2: implement against frozen boundary
    |       |
    |       +--> code follows frozen contract
    |       +--> tests validate scenarios
    |
    +--> Version analysis
            |
            +--> reconstruct boundary, scenarios, findings
            +--> show coverage and gaps

Self-Validation Gate
    |
    +--> contract must compile via build
    +--> scenario tests must pass
    |
    v
Review Package
    |
    +--> contract review: class diagram + C4
    +--> scenario review: sequence diagram
    +--> coverage review: decision table
    |
    v
User Review
```

## Choose the Mode

### Phase 1: Confirm Spec

Use this mode when the request changes architecture, public contract, canonical naming, module boundaries, or other shared collaboration baselines.

Do the following:

- Summarize the plan first, then inspect repo context, then implement.
- Keep the first pass thin, modular, and architecture-first.
- Produce `./docs/` material for review and design understanding.
- Produce `./spec/` and `./spec/testcases/` as the formal baseline for later implementation.
- Produce or update the repo's `.Abstract` contract project when that pattern exists.
- Make the spec precise enough to map directly to code contract.
- Make the validation scenarios precise enough to become executable tests, preferably unit tests.
- Require each stage boundary to have its own contract and tests.
- Package the boundary for review with class diagrams and C4 models.
- Package the scenarios for review with sequence diagrams.
- Package scenario coverage for review with a decision table.
- Stop for review/freeze before substantial `.Core` or host refactors.

Do not do the following:

- Treat `.Abstract` as frozen before the user confirms it.
- Mix a large behavior-fix round into a spec-shaping round unless the user explicitly wants them combined.
- Ask for review before the contract builds and the stage tests pass.

### Phase 2: Execute Against Frozen Spec

Use this mode when the spec and `.Abstract` are already frozen, or when the user explicitly asks to implement against the accepted baseline.

Do the following:

- Keep `./spec/` and `.Abstract` unchanged unless the user explicitly reopens Phase 1.
- Let the agent work autonomously through `.Core`, host projects, tests, and verification until review-ready.
- Prefer `spec/testcases` first when choosing verification targets.
- Add or update `*.Core.Tests` when a code-level delivery check is needed.
- Make implementation conform to the frozen contract so build failure reveals contract drift.
- Keep scenario validation executable so test failure reveals behavior drift.
- Require the stage to pass build and tests before review.
- Keep class diagrams and C4 models usable for boundary review.
- Keep sequence diagrams usable for scenario review.
- Keep the decision table updated so coverage and skipped space stay visible.

Do not do the following:

- Sneak contract changes into implementation rounds.
- Leave `.Core`, docs, spec, tests, and demo terminology drifting apart.
- Ask for review while contract conformance or scenario validation is still failing.

### Version Analysis: Reverse-Engineer a Commit

Use this mode when the user gives a commit hash or asks for phase-by-phase documentation.

Do the following:

- Use the exact commit snapshot as the primary source, not current `HEAD`.
- If the repo already has a phase-doc naming convention, reuse it.
- Otherwise default to `./docs/phase-{n}-{commit}/` with:
  - `README.md`
  - `c4-model.md`
  - `testcases/README.md`
  - `review-notes.md`
  - one testcase document per representative scenario
- Add `phase0 -> phase1` or `phase1 -> phase2` delta summaries in `README.md` when relevant.
- Keep review findings separate from overview text.
- Avoid backfilling later terminology into earlier snapshots when the older commit did not use it.
- Reconstruct contract boundaries with class diagrams and C4 models.
- Reconstruct runtime behavior with sequence diagrams.
- Reconstruct scenario coverage with a decision table that marks what is known, what is covered, and what remains outside current review scope.

For concrete output checklists, read `references/output-patterns.md`.

## Apply the Non-Negotiables

- Write chat responses, docs, and comments in Traditional Chinese when the repo or user asks for it.
- Record important proposed or accepted decisions in `./docs/decisions/` using the repo's decision structure.
- Keep canonical naming single-track.
  If a path, field name, or term changes, update source, docs, demo, spec, and tests together unless the user explicitly asks for compatibility.
- Explain why an unrelated request deserves to interrupt the roadmap before inserting it into the main flow.
- Use class diagrams and sequence diagrams when the user is validating naming, boundaries, or runtime interaction.
- Expand known scenarios into a decision table so the reviewer can see total coverage shape.
- Mark which decision-table rows are already covered, which are intentionally deferred, and which remain unknown.
- Do not chase meaningless 100% coverage.
  The goal is full visibility of the decision space, not blind saturation.
- Respect explicit de-prioritization.
  If the user marks a project or area as non-mainline, keep it readable but do not spend effort there unless later asked.

## Review and Verification

- Present review findings first, ordered by severity, with exact file references.
- Put open questions or assumptions after the findings.
- Put change summary last.
- If verification is incomplete, say exactly what was and was not validated.
- Run every `dotnet build` and `dotnet test` in the current repo with a single msbuild node: `-m:1`.
- Never run multiple msbuild sessions in parallel in the current repo.
- Treat build success as the minimum contract-conformance gate.
- Treat test success as the minimum scenario-validation gate.
- Do not present work for review until both gates pass for the current stage.

## References

- Read `references/source-priority.md` for source precedence and conflict rules.
- Read `references/output-patterns.md` for deliverable checklists and review/output patterns.
