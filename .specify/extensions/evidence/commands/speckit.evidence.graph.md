---
description: "Validate and render the task DAG; compute synthetic propagation."
---

# /speckit.graph.compute

Parse `specs/<feature>/tasks.md` and `specs/<feature>/tasks.deps.yml`,
validate the graph (acyclic, no dangling refs, every id present in both
files), and render `readiness/task-graph.json` + `readiness/task-graph.md`.

## How to invoke

```bash
.specify/extensions/evidence/scripts/python/compute-task-graph.py specs/<FEATURE_ID>
```

or via the audit runner in graph-only mode:

```bash
.specify/extensions/evidence/scripts/bash/run-audit.sh specs/<FEATURE_ID> --graph-only
```

## When to run

- Right after `/speckit.tasks` — confirms the initial DAG is well-formed
  before implementation begins.
- After every status change during `/speckit.implement` — refreshes `[S*]`
  propagation cheaply.
- Automatically as the `before_implement` hook (declared in the evidence
  extension's `extension.yml`) — refuses to start implement on a broken
  graph.

## What it validates

- Every `Tnnn` in `tasks.md` has a matching key in `tasks.deps.yml`.
- Every `Tnnn` in `tasks.deps.yml` has a matching task line in `tasks.md`.
- Every dep reference resolves to a known `Tnnn`.
- The graph is acyclic.
- No task depends on itself.

## What it computes

Effective status per task, under the propagation rule:

```
effective(T) =
    synthetic      if declared(T) == synthetic
    auto-synthetic if declared(T) == done AND any dep is (auto-)synthetic
    declared(T)    otherwise
```

Phase-checkpoint edges are auto-injected (every task in Phase N+1 gets an
implicit edge to the last foundation task of Phase N). These do not appear
in `tasks.deps.yml` and do not need to be written by hand.

## On failure

The script exits non-zero and writes the errors into `task-graph.md`'s
verdict block. Do not proceed with `/speckit.implement` until the graph
is clean.

Common failure modes and their fixes:

- **Dangling ref** — `tasks.deps.yml` references `Tnnn` that isn't in
  `tasks.md`. Add the task line or remove the ref.
- **Orphaned key** — `tasks.deps.yml` has a key for `Tnnn` that isn't in
  `tasks.md`. Remove the key or add the task line.
- **Cycle** — a set of tasks transitively depend on each other. The error
  message names the cycle path. Break it by removing one edge.
- **Duplicate task id** — the same `Tnnn` appears twice in `tasks.md`.
  Renumber one.

## Output

- `specs/<FEATURE_ID>/readiness/task-graph.json` — structured state.
- `specs/<FEATURE_ID>/readiness/task-graph.md` — mermaid diagram, ASCII
  view, status counts, propagation report with root-cause annotations.

Commit both files alongside the feature's other artifacts.
