# sk-Dashboard Constitution

## Core Principles

### I. Spec -> FSI -> Semantic Tests -> Implementation

Every non-trivial change MUST follow this order:

1. **Specify.** Feature spec names user-visible outcome, scope boundaries,
   change classification, public API impact, and verification approach.
2. **Sketch in FSI.** The intended public surface is drafted as a `.fsi`
   signature and exercised interactively in F# Interactive before any `.fs`
   implementation exists. API shape is validated by use, not by hope.
3. **Semantic tests for FSI.** Tests MUST exercise the API through the same
   FSI surface a human or script would use: load the packed library (or a
   prelude script) and call the public functions. Tests assert behavior,
   not internals.
4. **Implement.** Write the `.fs` body against the now-stable signature and
   passing tests.

Rationale: FSI is the honest audience. If the shape is awkward in FSI, it is
awkward in production. Designing through FSI catches API mistakes before
`.fs` code exists to defend them.

### II. Visibility Lives in `.fsi`, Not in `.fs`

Every public F# module MUST have a corresponding `.fsi` signature file. The
`.fsi` is the sole declaration of the module's public surface. Symbols
omitted from the `.fsi` are automatically private - the F# compiler enforces
this.

Therefore: `.fs` files MUST NOT contain the `private`, `internal`, or
`public` access modifiers on top-level bindings. Visibility is determined by
presence or absence in the `.fsi`, not by keywords scattered across `.fs`.
Surface-area baselines MUST be maintained per public module and validated by
an automated test.

Rationale: Two sources of truth for visibility is one too many. `.fsi`
already gives the compiler the full picture; access modifiers in `.fs` just
invite drift.

### III. Idiomatic Simplicity Is the Default

Code SHOULD prefer the plainest F# that solves the problem: functions over
classes, records over hierarchies, pipelines over mutation, the standard
library over clever abstractions. A reader should not need a textbook to
follow ordinary code.

Complex features MAY be used, but their use MUST be justified in the
feature's spec or plan. The following require explicit justification:

- Custom operators beyond the F# standard set
- Statically-resolved type parameters (SRTP) and inline tricks that force it
- Reflection and dynamic dispatch
- Non-trivial computation expressions (beyond `async`, `task`, `option`, `result`, `seq`)
- Type providers
- Active patterns beyond single-case or simple discriminants

If such a feature appears without matching justification, the reviewer
treats it as a spec defect, not a code defect.

**Mutation is allowed when it is the simpler or faster code.** `mutable`
bindings, `for` / `while` loops, and `ref` cells MAY be used when they
are demonstrably plainer than the immutable alternative or are needed
on a measured hot path. "Pipelines over mutation" is the default, not a
prohibition: a single accumulator that is never aliased, an inner loop
over a buffer, or a performance-critical routine is fine to write with
`mutable`. Disclose the reason at the use site with a one-line comment
(e.g. `// mutable: hot path`, `// mutable: avoids deep accumulator
threading`) so a reader doesn't waste effort "fixing" it.

**Recursion is for branching structure, not for hiding state.** `let
rec` is the right tool when the problem is genuinely recursive -
state-machine transitions, tree / graph walks, branching evaluators,
parser combinators. It is the wrong tool when its only purpose is to
thread an accumulator through self-calls in order to avoid a `mutable`.
If the recursion exists solely to dodge mutation, the `mutable` is the
clearer code; prefer it.

Rationale: Complexity compounds in F# because the language rewards
expressive tricks. A simplicity bias keeps code legible to future maintainers
who are not the current author. Dogmatic immutability - recursion
gymnastics in place of an obvious loop, or a fold-with-state where a
`mutable` would read straight through - is itself a form of cleverness
this principle exists to discourage.

### IV. Synthetic Evidence Requires Loud, Repeated Disclosure

Synthetic evidence - mocks, stubs, fakes, hardcoded fixtures, in-memory
substitutes, `NotImplementedException` placeholders, `failwith "TODO"`,
canned responses, or any test that exercises only literal data - MAY be used
when real evidence is unavailable or prohibitively expensive, AND a
real-evidence path is either planned or explicitly documented as infeasible.

Every synthetic use MUST be disclosed at every surface it appears:

1. **Task level.** The task is marked `[S]` (synthetic-only) in `tasks.md`,
   never `[X]`. Any task whose dependency is `[S]` is automatically marked
   `[S*]` by the evidence audit.
2. **Code level.** A comment at the use site names the fact and the reason,
   e.g. `// SYNTHETIC: no staging DB yet; replaced once US-17 lands`.
3. **Test level.** Test names contain the token `Synthetic`, and any test
   file whose fixtures are wholly synthetic opens with a banner comment
   `(* SYNTHETIC FIXTURE: ... *)`.
4. **Spec level.** The originating user story names the synthetic dependency
   and the real-evidence path that will replace it (or argues why real
   evidence is infeasible).
5. **PR level.** The PR description enumerates every `[S]` task and links
   the justification.

Duplication is deliberate. Synthetic evidence tends to hide; repeating its
disclosure at every visible surface is how it stays visible. Prefer
explicit, ugly literals (`let syntheticUserId = 42 // SYNTHETIC`) over
clever factories that make synthetic data feel real.

A feature MUST NOT be declared merge-ready while any `[S]` or auto-propagated
`[S*]` task remains, and MUST NOT be declared merge-ready while any
diff-scan hit is unresolved. The evidence audit runs as an `after_implement`
hook and hard-blocks merge readiness on either condition.

An explicit `--accept-synthetic` override is available for bounded cases
(staged rollout, upstream dependency not yet ready). It requires written
justification in the PR description and is logged to
`readiness/synthetic-evidence.json`. The audit still reports failure; the
override is a human decision, not a silenced gate.

Rationale: Synthetic evidence is the quiet failure mode of "passing" tests.
Loud, redundant disclosure is the only practice that scales past the
attention of the author.

### V. Test Evidence Is Mandatory

Behavior-changing code MUST include automated tests that fail before the
change and pass after. Prefer tests that run against real dependencies (real
DB, real filesystem, real network where safe); fall back to synthetic only
under Principle IV's disclosure regime.

Tests blocked by out-of-scope issues MUST be marked skipped (task `[-]`,
`[<Skip>]` attribute, or the test framework's skip mechanism) with written
rationale. Never mark a failing test as passed. Never weaken an assertion to
green a build - weaken the scope instead, and document it.

### VI. Observability and Safe Failure

Operationally significant events (startup, subsystem initialization,
asset/IO failure, recovery paths) MUST emit structured diagnostics with
actionable context. Errors MUST fail fast or degrade explicitly; silent
failure and swallowed exceptions are forbidden in critical paths.

## Change Classification

Every feature declares a tier in its spec:

- **Tier 1 (contracted change)** - adds, removes, or modifies public API
  surface; introduces new dependencies; changes inter-project contracts
  (`.proto`, OpenAPI); alters observable behavior covered by existing specs.
  Requires the full artifact chain: spec, plan, `.fsi` updates, surface-area
  baseline updates, test evidence, and documentation updates.
- **Tier 2 (internal change)** - refactors, performance, internal cleanup
  with no behavioral change. Requires spec and tests; `.fsi` and baselines
  remain untouched.

A Tier 1 change that fails to update `.fsi` or baselines is a defect,
regardless of whether tests pass.

## Engineering Constraints

- F# on .NET is the exclusive stack. Cross-language integration uses gRPC or
  OpenAPI over separate projects.
- Every public `.fs` module requires a curated `.fsi`.
- Surface-area baseline files are required for each public module.
- Public API changes document compatibility impact and migration guidance.
- Dependencies are minimized; each new dependency states need, version
  pinning strategy, and maintenance owner.
- Pack output location: `~/.local/share/nuget-local/`
- Structured-logging library: not yet selected; see ADR when chosen.
- Project-specific constraints: terminal dashboard for local Speckit
  repositories; supports editor-integrated terminals including Emacs vterm.
- Installed global tool updates MUST bump the package version in
  `Directory.Build.props` before packing. Repack to the local NuGet source,
  then run `dotnet tool update -g sk-Dashboard --add-source
  ~/.local/share/nuget-local`. Rebuilding and reinstalling the same package
  version is reserved for exceptional local recovery only.

## Workflow & Quality Gates

Work MUST pass these gates in order:

1. **Specification gate** - spec is complete and bounded, names its Tier,
   public-API impact, and verification approach.
2. **Planning gate** - plan translates the constitution into concrete
   design; Tier 1 plans include `.fsi` contract updates.
3. **Task gate** - tasks are story-grouped; `tasks.deps.yml` is emitted
   alongside `tasks.md`; task graph is acyclic with no dangling refs.
4. **Implementation gate** - declared task statuses follow the legend; `[S]`
   is used whenever Principle IV applies.
5. **Evidence gate** - the `after_implement` audit produces verdict PASS
   with no remaining `[S]` / `[S*]` and no unresolved diff-scan hits, or
   every exception is covered by a logged `--accept-synthetic` override
   with written justification.

Any intentional deferral MUST be explicit in the spec or plan and scoped as
a bounded follow-up.

## Governance

This constitution overrides conflicting local habits, informal preferences,
and agent prompts for work in this repository. Compliance review MUST occur
at specification, planning, task generation, implementation review, and
merge readiness review.

**Amendment procedure:** PR with rationale and migration impact; maintainer
review required.

**Versioning policy:**

- MAJOR - backward-incompatible governance changes or principle removals
- MINOR - new principles, new mandatory gates, or materially expanded
  obligations
- PATCH - clarifications that do not change the meaning of the rules

Amendments MUST update dependent templates and guidance files in the same
change. When the constitution and a template disagree, the constitution is
correct and the template is defective until synchronized.

**Version**: 1.0.0 | **Ratified**: 2026-04-25 | **Last Amended**: 2026-04-25
