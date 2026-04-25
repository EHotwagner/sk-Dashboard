---
description: "Emit tasks.md and tasks.deps.yml in lockstep, then validate the DAG."
---

# /speckit.tasks

Generate the feature's task breakdown from its spec and plan. This preset
requires TWO files, both in `specs/[FEATURE_ID]/`:

1. **`tasks.md`** — the human checklist. Status legend:
   - `[ ]` pending / `[X]` done (real evidence) / `[S]` synthetic-only /
     `[F]` failed / `[-]` skipped.
   - Never write `[S*]` yourself. That marker is computed by the evidence
     audit from the DAG; writing it by hand will confuse the script.

2. **`tasks.deps.yml`** — the dependency topology. Sibling to `tasks.md`.
   Every Tnnn id in `tasks.md` MUST appear as a key here, even if its deps
   list is empty.

Use the preset's `tasks-template.md` and `tasks.deps-template.yml` as
starting points; replace the example task bodies with real work items
derived from the spec and plan.

## Discipline

- **Lockstep emission.** You MUST write both files in the same turn. Never
  emit `tasks.md` without `tasks.deps.yml`, and vice versa. If you only
  have partial information, write placeholder deps (`Tnnn: []`) and note
  which ones need review in your summary.
- **Phase-checkpoint edges are implicit.** The graph compute script
  auto-injects an edge from every task in Phase N+1 to the last foundation
  task of Phase N. You do NOT repeat those edges in the yml — write only
  non-phase cross-edges (e.g., `T015: [T011, T013]` says US1 impl depends
  on US1 tests and US1 fixtures).
- **Story grouping.** Tasks belong to a phase (Phase 1..N) and optionally a
  user story (`[US1]`, `[US2]`, ...). Keep phases sequential; stories
  within a phase may run in parallel.
- **Tier annotation.** Mark each task `[T1]` or `[T2]` if the phase
  classification differs from the spec's overall tier. Omit when it
  matches.
- **Parallel-safe marker.** `[P]` means "no deps inside this phase" — the
  script can verify it; you SHOULD emit it as a hint.
- **Synthetic-evidence inventory.** Include the empty Synthetic-Evidence
  Inventory table from the template. It will grow as `/speckit.implement`
  adds `[S]` tasks.

## Validation

Immediately after writing both files, run:

```bash
.specify/extensions/evidence/scripts/bash/run-audit.sh --graph-only
```

(or invoke `/speckit.graph.compute` if the extension is installed).

This validates:
- Every Tnnn in `tasks.md` has a matching key in `tasks.deps.yml`.
- Every dep reference resolves to a known Tnnn.
- The graph is acyclic.

Report any failures to the user immediately; refuse to declare the tasks
phase complete until the DAG is clean.

## If the evidence extension is not installed

Fall back to emitting both files without running the validator. Warn the
user: *"The evidence extension is not installed, so the DAG cannot be
validated. Run `specify extension add evidence` to enable
`speckit.graph.compute` and `speckit.evidence.audit`."*
