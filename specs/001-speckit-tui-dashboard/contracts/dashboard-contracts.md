# Dashboard Contracts

## Command-Line Interface

Executable:

```bash
sk-dashboard [PROJECT_PATH] [--no-auto-checkout] [--config PATH] [--refresh-interval MS]
```

Arguments:
- `PROJECT_PATH`: optional repository root. Defaults to the current working directory.

Options:
- `--no-auto-checkout`: disables startup checkout for diagnostic runs. The default behavior remains automatic checkout of the latest feature branch.
- `--config PATH`: loads hotkey configuration from a specific file instead of the global user path.
- `--refresh-interval MS`: sets polling fallback interval. Values below the implementation minimum are rejected with a visible diagnostic.

Exit behavior:
- Normal quit returns `0`.
- Invalid command-line arguments return non-zero and print a concise error to stderr.
- Missing Speckit artifacts, missing feature branches, malformed task data, and checkout failures do not by themselves terminate the app.

## Global Hotkey Configuration

Format: JSON object with a `bindings` array.

```json
{
  "version": 1,
  "bindings": [
    { "command": "feature.previous", "key": "k" },
    { "command": "feature.next", "key": "j" },
    { "command": "refresh", "key": "r" },
    { "command": "quit", "key": "q" }
  ]
}
```

Required behavior:
- Unknown commands are diagnostics.
- Unsupported key sequences are diagnostics.
- Duplicate active key sequences are conflicts.
- Defaults remain active for commands not overridden.
- User overrides are activated only after validation leaves zero unresolved conflicts.

Primary command identifiers:
- `feature.previous`
- `feature.next`
- `feature.checkout`
- `story.previous`
- `story.next`
- `task.previous`
- `task.next`
- `pane.next`
- `pane.previous`
- `details.open`
- `details.close`
- `refresh`
- `hotkeys.reload`
- `quit`

## Project State Contract

The core state loader returns a dashboard snapshot rather than throwing for normal missing or malformed project data.

Snapshot fields:
- `repositoryRoot`
- `currentBranch`
- `features`
- `selectedFeatureId`
- `stories`
- `selectedStoryId`
- `plan`
- `taskGraph`
- `selectedTaskId`
- `panes`
- `diagnostics`
- `lastRefreshedAt`

Required behavior:
- No `specs/` directory returns an empty feature state with diagnostics severity `info`.
- Feature branches without artifact directories remain visible as features.
- Artifact parse failures return partial state with diagnostics severity `warning` or `error`.
- Checkout failures are represented in `selectedFeature.checkoutState` and shown in diagnostics.

## Task Graph Contract

Input:
- Selected story ID.
- Parsed task collection.
- Parsed task dependency references.

Output:
- Directed graph containing selected-story tasks.
- Dependency-chain tasks required by those selected-story tasks, including cross-story dependencies.
- Diagnostics for missing references, duplicate task IDs, and cycles.

Required behavior:
- Raw task status text is preserved on every graph node.
- Cycles are reported clearly and do not erase recoverable task nodes.
- Unknown dependency references are represented as diagnostics and may be rendered as missing-reference placeholders.

## Rendering Contract

Required panes:
- Features/status pane.
- User stories pane on the left side when space permits.
- Plan pane separate from story and task graph content.
- Task graph pane.
- Task details pane.
- Diagnostics/status line or pane.

Required behavior:
- Every pane is keyboard reachable.
- Compact layouts may reduce simultaneous visibility but must preserve access through focus changes, scrolling, or alternate layout.
- Rendering must avoid requiring mouse input, alternate GUI windows, or terminal features unavailable in common editor-integrated terminals.
