---
description: "Implement tasks with synthetic-evidence marking discipline (Principle IV)."
---

# /speckit.implement

Execute the feature's tasks against the plan. Update `tasks.md` as you go.

## Status marking discipline

Use the status legend from the template exactly:

- `[ ]` — not started.
- `[X]` — **done with real evidence.** The code paths that will run in
  production were exercised. Tests used real dependencies (real DB, real
  filesystem, real network) or a previously-approved synthetic fixture
  that is itself listed in the Synthetic-Evidence Inventory as acceptable.
- `[S]` — **done with synthetic evidence only.** Use this whenever the
  task's "pass" depends on ANY of:
  - a mock, stub, fake, or in-memory substitute
  - a `NotImplementedException`, `failwith "TODO"`, `raise ()`, or
    equivalent placeholder
  - hardcoded literals where production code will need a real data source
  - a test that exercises only synthetic fixtures
  - a dependency on another `[S]` task whose synthetic nature propagates
    (you do not need to detect this manually — the evidence audit
    computes `[S*]` propagation — but BE HONEST about the direct cases).
- `[F]` — **failed.** Implementation attempted and did not pass. Leave the
  diagnostics in place; do not quietly retry.
- `[-]` — **skipped.** Requires written rationale either in the task line
  or in the Synthetic-Evidence Inventory / Deferral Notes section.

**Never mark a task `[X]` if any of the `[S]` conditions apply.** The
evidence audit will catch many such cases via diff-scan, but the agent is
expected to be honest about direct declarations. Dishonesty undermines the
whole synthetic-evidence regime (Principle IV).

## Vertical-slice rule (US phases)

A task tagged `[US*]` may only be marked `[X]` when the user-facing
surface was actually exercised end-to-end. "Exercised" means one of:

- An FSI transcript captured under `readiness/` that drives the new
  behavior through its public entry point — not through internal helpers.
- A smoke run of the host application (CLI invocation, GUI launch, HTTP
  request) that touches the new code path, with the artifact (log,
  screenshot, response body) saved under `readiness/`.
- A semantic test that loads the **packed** library or runs the host
  binary and exercises the user-reachable path — not a unit test against
  domain modules in isolation.

A diff that touches only `Domain/`, `Core/`, `Models/`, or equivalent
internal layers is **never** sufficient evidence for `[X]` on a `[US*]`
task. The story isn't done when the model compiles; it's done when the
user can reach it. If wire-up to the UI / CLI / API surface is missing,
the honest status is `[ ]` (continue working) or `[S]` (disclose the
gap and create a tracking issue for the real wire-up).

This rule is in addition to the synthetic-evidence checks above. A task
can fail the vertical-slice rule without involving any mocks at all —
domain code that nothing calls is its own failure mode.

## Synthetic-evidence disclosures (Principle IV)

When you emit an `[S]` task, you MUST also:

1. **Code-level disclosure.** Add a `// SYNTHETIC:` comment at the use
   site, naming the reason and (if known) the real-evidence path. Example:
   ```fsharp
   let userRepo = InMemoryUserRepo()  // SYNTHETIC: staging DB not provisioned; real repo in US-17
   ```
2. **Test-level disclosure.** Test names exercising the synthetic surface
   contain the token `Synthetic`. Example:
   ```fsharp
   [<Test>] let ``Signup.createUser_Synthetic_persists in-memory`` () = ...
   ```
   For whole test files that are synthetic-only, open with a banner:
   ```fsharp
   (* SYNTHETIC FIXTURE: all tests in this file use canned SMTP responses. *)
   ```
3. **Inventory update.** Add a row to the Synthetic-Evidence Inventory
   table in `tasks.md`:
   - Task id
   - Reason
   - Real-evidence path (or "infeasible, see spec §X")
   - Tracking issue (create one if the real-evidence path is a future
     feature)

## Workflow, per task

1. Mark the task `in_progress` in your own head (not in `tasks.md` — the
   file uses the five-state legend only).
2. Read the task's deps from `tasks.deps.yml`; confirm all deps are `[X]`
   or `[S]`. If any dep is `[ ]`, `[F]`, or `[-]`, stop and raise it.
3. Implement the task per the plan.
4. Run the verification appropriate for the phase (tests, baseline check,
   FSI exercise, …). For tasks tagged `[US*]`, the verification MUST
   include a user-reachable exercise — see the Vertical-slice rule
   above. A green unit test on the domain layer is not enough.
5. Update the status in `tasks.md`. Before writing `[X]` on a `[US*]`
   task, confirm the vertical-slice rule is satisfied; if not, the
   honest status is `[ ]` or `[S]`. If `[S]`, add the code-level,
   test-level, and inventory disclosures before moving on.
6. **Re-run `speckit.graph.compute`** after every status change. This
   refreshes `readiness/task-graph.json` and recomputes `[S*]`
   propagation. It's cheap (milliseconds).
7. Move to the next task.

## Visibility discipline (Principle II)

- Never write `private`, `internal`, or `public` on a top-level F#
  binding. Visibility lives in the `.fsi` signature file.
- If a task needs to change the public surface, the `.fsi` update is part
  of the same task — not a follow-up.

## Simplicity discipline (Principle III)

Before reaching for a "clever" F# feature (custom operators, SRTP,
reflection, non-trivial computation expressions, type providers, non-
obvious active patterns), confirm the feature is justified in the spec or
plan. If it isn't, either simplify or stop and raise the justification
gap.

## Stop conditions

Stop and ask the user when:

- A task's spec guidance conflicts with its code. The spec wins (Principle
  III: complex features require justification).
- A test fails in a way that would require weakening an assertion or
  adding `[<Skip>]` to pass. Never weaken; surface the failure.
- A dependency in `tasks.deps.yml` points to a task that doesn't exist.
  Fix the yml before proceeding.
