# Data Model: Markdown Rendering and Constitution Hotkey

## Markdown Document

Represents Markdown-backed content shown in a full/detail document surface.

**Fields**:
- `Title`: display title for the surface.
- `RawContent`: original file or artifact text.
- `SourceLocation`: optional source path and line.
- `Kind`: constitution, plan, task, story, feature, or other detail document.
- `RenderStatus`: formatted, fallback plain text, empty, missing, unreadable, or render failed.
- `Diagnostics`: non-fatal diagnostics produced while loading or rendering.

**Validation rules**:
- Missing/unreadable sources produce a diagnostic and fallback message.
- Empty content produces an explicit empty-document message.
- Rendering failures preserve escaped raw text when content exists.

## Constitution View

Dedicated dashboard view for the current project constitution.

**Fields**:
- `Document`: loaded Markdown document or a fallback document describing why constitution is unavailable.
- `PreviousContext`: selected feature, story, task, focused pane, and any active full/detail context needed to return cleanly.
- `Viewport`: line and column offsets for keyboard navigation.

**State transitions**:
- `Closed -> Open`: pressing the constitution command reads `.specify/memory/constitution.md` from the current repository root and builds a Markdown document.
- `Open -> Open`: scroll commands update the viewport while preserving document identity.
- `Open -> Closed`: close command restores previous dashboard context.
- `Open -> Open`: reopening after close reads the file again, so changed content is reflected.

## Hotkey Command

Keyboard-triggered dashboard action for opening the constitution.

**Fields**:
- `Command`: `ConstitutionOpen`.
- `DefaultKeySequence`: `C`.
- `Scope`: dashboard/global scope following existing hotkey conventions.
- `Label`: user-facing help/discovery label.
- `ConflictStatus`: produced by existing hotkey validation.

**Validation rules**:
- The default binding participates in existing duplicate/conflict detection.
- User-configured bindings override the default through existing preference loading.

## Render Failure Diagnostic

Non-fatal diagnostic for constitution access or Markdown rendering failures.

**Fields**:
- `Severity`: warning or error depending on recoverability.
- `Message`: concise user-facing summary.
- `Source`: attempted constitution path or document source when available.
- `FailureKind`: missing, empty, unreadable, malformed/unsupported Markdown fallback, or renderer exception.

**Validation rules**:
- Diagnostics must avoid unrelated environment details.
- Missing/empty/unreadable/render-failed documents must not terminate dashboard execution.
