# Data Model: Theme Settings and Checklist Hotkey

## App Theme

Named bundle of compact dashboard/TUI presentation settings.

**Fields**:
- `Id`: stable identifier, unique within the app theme family.
- `DisplayName`: user-facing name shown in settings.
- `Source`: built-in, custom file path, or fallback.
- `ModeBehavior`: fixed light, fixed dark, or default resolver.
- `Table`: border style, header style, compact cell behavior, selection style, and alternate row shading flag.
- `Colors`: foreground, muted, background, panel, selection, status, warning, error, success, accent, and related readable UI roles.
- `ValidationStatus`: valid, valid with replacements, ignored, duplicate, incomplete, unreadable, wrong family, or unavailable.
- `Diagnostics`: validation and loading feedback.

**Validation rules**:
- Built-in `default`, `light`, and `dark` app themes always exist.
- Built-in app themes keep alternate row shading off unless a user setting explicitly enables it.
- Required foreground/background role pairs must remain readable or be replaced by safe fallback values.
- Unknown future fields are ignored with known valid fields preserved.
- Duplicate identifiers keep the first valid theme by deterministic load order and report diagnostics for later duplicates.

## Markdown Theme

Named bundle of Markdown document rendering colors and spacing.

**Fields**:
- `Id`: stable identifier, unique within the Markdown theme family.
- `DisplayName`: user-facing name shown in settings.
- `Source`: built-in, custom file path, or fallback.
- `ModeCompatibility`: follows resolved app mode, fixed compatible palettes, or explicit light/dark values.
- `ElementColors`: normal text, headings, emphasis, strong text, links, inline code, code blocks, block quotes, list markers, checked items, unchecked items, notes, and muted text.
- `Spacing`: spacing before/after sections, headings, paragraphs, lists, and code blocks.
- `ValidationStatus`: valid, valid with replacements, ignored, duplicate, incomplete, unreadable, wrong family, or unavailable.
- `Diagnostics`: validation and loading feedback.

**Validation rules**:
- Built-in `plain` and `default` Markdown themes always exist.
- `plain` preserves the current Markdown appearance as closely as possible.
- `default` improves human readability with distinct element colors and controlled spacing.
- Spacing is clamped for short terminal heights so content remains navigable.
- Markdown colors must remain readable against the resolved app background unless explicit compatible values are supplied.

## Theme Family

Classification that prevents app themes and Markdown themes from being applied to the wrong surface.

**Fields**:
- `Family`: app or markdown.
- `BuiltInThemes`: ordered list of built-in themes for the family.
- `CustomThemeFolder`: family-specific custom theme folder.
- `Diagnostics`: folder and discovery diagnostics.

**Validation rules**:
- Wrong-family files are ignored for the current family and reported as validation feedback.
- Missing, empty, or unreadable folders do not prevent built-in themes from loading.

## Theme Selection

User's persisted choice for app theme and Markdown theme.

**Fields**:
- `SelectedAppThemeId`: saved app theme identifier.
- `SelectedMarkdownThemeId`: saved Markdown theme identifier.
- `ResolvedAppTheme`: effective app theme after fallback.
- `ResolvedMarkdownTheme`: effective Markdown theme after fallback.
- `FallbackStatus`: no fallback, custom missing, invalid selected theme, or unreadable selection.
- `LastAppliedAt`: optional timestamp/version used by live reload or settings sessions.

**State transitions**:
- `Loaded -> Applied`: valid saved selections resolve to available themes.
- `Loaded -> FallbackApplied`: missing or invalid selected custom themes fall back to the matching built-in `default`.
- `Editing -> Saved`: settings save persists selected theme ids and reloads visible surfaces.
- `Editing -> Discarded`: settings discard restores loaded selections.
- `ExternalChanged -> Reloaded`: live reload applies valid external theme selection changes when the current settings surface is not dirty.

## Resolved Display Mode

Effective light or dark mode used by app and Markdown theme rendering.

**Fields**:
- `RequestedMode`: selected app theme behavior or app-level mode setting.
- `EnvironmentPreference`: detected light/dark preference when available.
- `EffectiveMode`: light or dark.
- `Reason`: manual selection, app setting, environment preference, or fallback.

**Validation rules**:
- Manual `light` and `dark` selections override app/environment preferences.
- Built-in app `default` resolves by app-level setting first, then environment preference, then readable fallback.
- If environment preference cannot be detected, the fallback must still be readable.

## Theme Validation Feedback

User-facing and diagnostic feedback for theme discovery, parsing, validation, and fallback.

**Fields**:
- `Severity`: info, warning, or error.
- `Family`: app or markdown.
- `ThemeId`: optional theme id.
- `Source`: theme file path, folder path, or built-in source.
- `Message`: concise user-facing message.
- `FailureKind`: missing, unreadable, invalid JSON, incomplete, duplicate id, duplicate display name, wrong family, unreadable colors, unavailable selected theme, or unsupported value.

**Validation rules**:
- Feedback avoids unrelated environment details.
- Critical presentation failures keep the last usable theme active.
- Settings surfaces show validation feedback without blocking valid choices.

## Checklist View

Dashboard view that lists and displays Spec Kit checklist files for the active feature.

**Fields**:
- `AvailableChecklists`: discovered checklist files for the active feature.
- `SelectedChecklist`: current checklist file when one is selected.
- `Document`: themed Markdown document or fallback empty/error document.
- `PreviousContext`: selected feature, story, task, focused pane, and any active full/detail/settings context needed to return cleanly.
- `Viewport`: scroll offsets for keyboard navigation.
- `Diagnostics`: missing, empty, unreadable, malformed, or too-large checklist feedback.

**State transitions**:
- `Closed -> Listing`: pressing the checklist command discovers active-feature checklist files.
- `Listing -> Reading`: selecting a checklist opens it as a Markdown-backed document.
- `Reading -> Listing`: back returns to the checklist list.
- `Listing/Reading -> Closed`: close restores previous dashboard context.
- `Closed -> Empty`: no checklist files produces a non-fatal empty-state message.

## Hotkey Command

Keyboard-triggered dashboard action for opening checklists.

**Fields**:
- `Command`: checklist-open command.
- `CommandId`: stable id `checklists.open`.
- `DefaultKeySequence`: `L`.
- `Scope`: dashboard/global scope following existing hotkey conventions.
- `Label`: user-facing help/discovery label.
- `ConflictStatus`: produced by existing hotkey validation.

**Validation rules**:
- The default binding must not conflict with current built-in defaults.
- User-configured bindings override the default through existing preference loading.
- The command appears in existing hotkey discovery/customization surfaces.
