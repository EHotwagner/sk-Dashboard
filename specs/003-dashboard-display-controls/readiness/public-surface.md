# Public Surface Impact

- `Domain.fsi`: adds `DashboardVersionDisplay`, `FullScreenTarget`, `FullScreenModal`, `DashboardSnapshot.Version`, `DashboardSnapshot.FullScreen`, `RowStripeOdd`, `RowStripeEven`, and `Domain.resolveDashboardVersion`.
- `Hotkeys.fsi`: adds `FullScreenFeature`, `FullScreenStory`, `FullScreenPlan`, and `FullScreenTask` command cases.
- Existing preference and layout types remain source-compatible except for callers constructing `DashboardSnapshot` records directly.

