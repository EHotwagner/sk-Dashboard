# Research: Theme Settings and Checklist Hotkey

## Decision: Extend the existing dashboard preferences file for saved theme selections

**Rationale**: Prior settings work already defines a user-owned dashboard config path, validation diagnostics, save/discard behavior, and live reload semantics. Saving `appThemeId` and `markdownThemeId` in the same preferences contract keeps in-app settings, hotkey preferences, table preferences, and theme selections coherent.

**Alternatives considered**:
- A separate theme selection file was rejected because it would duplicate stale-save and validation behavior.
- Environment variables were rejected because selections need in-app editing, persistence, validation feedback, and save/discard behavior.

## Decision: Keep app themes and Markdown themes as separate families

**Rationale**: App themes control compact TUI surfaces such as tables, borders, status colors, backgrounds, and selection states. Markdown themes control document element colors and vertical spacing. Keeping the families separate prevents a readable document theme from expanding dense tables and lets users tune document readability without changing the surrounding dashboard.

**Alternatives considered**:
- One combined theme object was rejected because it couples compact table settings with document typography/spacing.
- Per-surface ad hoc settings were rejected because the user requested named bundles with display names.

## Decision: Built-in app `default` resolves to light or dark instead of acting as a third fixed palette

**Rationale**: The spec defines `default` as honoring app or operating environment preference and falling back to a readable mode. Modeling it as a resolver preserves manual `light` and `dark` choices while keeping a stable selection that can adapt when the environment changes.

**Alternatives considered**:
- A static default palette was rejected because it would not honor light/dark preferences.
- Requiring terminal OS detection in all cases was rejected because some terminals do not expose reliable mode information.

## Decision: Custom themes are discovered from family-specific theme folders

**Rationale**: Separate folders make it clear whether a theme is intended for app UI or Markdown rendering and simplify validation. Invalid, unreadable, duplicate, incomplete, or wrong-family files can be diagnosed without blocking valid themes from the other family.

**Alternatives considered**:
- A single mixed theme folder was rejected because family validation and wrong-family feedback become less predictable.
- Compile-time bundled custom themes were rejected because the feature requires user/team-shareable themes without code changes.

## Decision: Use a tolerant JSON theme definition contract

**Rationale**: The project already parses JSON preferences and the requirements say unknown future settings must not prevent known valid settings from being used. A versioned JSON shape with `family`, `id`, `displayName`, and family-specific settings supports forward compatibility while keeping validation explicit.

**Alternatives considered**:
- YAML/TOML were rejected because they add dependencies or parser surface for little benefit.
- F# script/theme plugins were rejected because executing user-supplied code is unnecessary and unsafe for presentation settings.

## Decision: Validate readability and fall back per family

**Rationale**: Theme settings can make foreground and background indistinguishable. Validation must reject or safely replace unreadable roles and keep the last usable presentation. If a saved custom theme disappears, the app family falls back to built-in `default` and the Markdown family falls back to built-in `default`, with clear feedback.

**Alternatives considered**:
- Applying invalid colors as-is was rejected because it can make the dashboard unusable.
- Failing startup on invalid themes was rejected by the safe-failure requirements.

## Decision: Add a dedicated checklist hotkey command to existing hotkey infrastructure

**Rationale**: The Core hotkey contract already exposes command ids, default bindings, labels, preference loading, discovery, and conflict diagnostics. Adding a checklist-open command there ensures the new hotkey participates in customization and conflict handling.

**Alternatives considered**:
- Hard-coding the key in Dashboard input handling was rejected because it bypasses existing hotkey discovery and user overrides.
- Reusing the constitution hotkey was rejected because checklists are a separate workflow and requested as a dedicated action.

## Decision: Render checklists through the selected Markdown theme

**Rationale**: Checklist files are Markdown-backed documents with headings, notes, and checked/unchecked items. Routing them through the same themed Markdown document path satisfies checklist readability while honoring app light/dark mode and preserving compact table behavior elsewhere.

**Alternatives considered**:
- A checklist-only renderer was rejected because it duplicates Markdown rendering and theming logic.
- Plain checklist text only was rejected because the feature requires checklist item state, headings, and notes to be clearly distinguished.
