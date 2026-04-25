---
description: "Fill the constitution template while respecting LOCKED markers."
---

# /speckit.constitution

Populate `.specify/memory/constitution.md` from the preset's
`constitution-template.md`. The template contains three kinds of sections,
marked by HTML comments:

- `<!-- REQUIRED -->` — placeholders MUST be filled. Ask the user for each.
- `<!-- TAILORABLE -->` — tune per project. Ask the user what's appropriate
  for this project's domain and stack. You MAY add or reword bullets within
  these sections.
- `<!-- LOCKED -->` — do NOT modify these sections. The six Core Principles,
  Change Classification, Workflow & Quality Gates, and Governance sections
  are shared doctrine across every project using this preset. If the user
  explicitly asks to modify a locked section, pause and confirm: *"This
  section is marked LOCKED because it's shared across every project using
  the fsharp-opinionated preset. Are you sure you want a per-project
  amendment, or should this become an upstream preset change?"*

## Placeholders to fill

The REQUIRED section uses these placeholders. Ask for each:

- `[PROJECT_NAME]` — the project's display name (often matches the repo name).
- `[CONSTITUTION_VERSION]` — start at `1.0.0` for a new project.
- `[RATIFICATION_DATE]` — today's date in `YYYY-MM-DD`.
- `[LAST_AMENDED_DATE]` — same as ratification on first write.

The TAILORABLE section uses these placeholders. Offer reasonable defaults
and let the user accept or override:

- `[PACK_OUTPUT_PATH]` — default `~/.local/share/nuget-local/`.
- `[LOGGING_LIBRARY]` — default "not yet selected; see ADR when chosen."
  Common F# choices: Serilog, Microsoft.Extensions.Logging, Logary.
- `[PROJECT_CONSTRAINTS]` — any project-specific rules (runtime target,
  deployment target, supported OS). If none, write "None beyond the
  defaults above."

## Output

- Write the filled constitution to `.specify/memory/constitution.md`.
- Remove the HTML comment markers from the final output (they're authoring
  aids, not reader content).
- Do NOT remove the LOCKED-section text itself. Only the `<!-- LOCKED -->`
  marker lines go.
- Produce a short summary for the user naming (a) what placeholders were
  filled, (b) what TAILORABLE choices were made, and (c) confirming no
  LOCKED sections were modified.

## If the constitution file already exists

Do not overwrite silently. Diff the existing file against the current
template and offer three options:

1. Update only REQUIRED/TAILORABLE placeholders in place (preserve any
   existing tailoring).
2. Refresh from the current template, preserving the existing project's
   TAILORABLE choices (warn if LOCKED sections have diverged — that's a
   sign the preset was amended upstream).
3. Abort and let the user edit manually.
