# Unsupported Scope And Safe Failure

- Missing version metadata shows `vunknown` and records a warning diagnostic from `Domain.resolveDashboardVersion`.
- Missing feature, story, plan, or task full-screen targets render a readable modal message with close guidance.
- Missing or unreadable source artifact text is rendered as missing/unreadable text in the modal instead of failing startup.
- Invalid or low-contrast stripe preferences emit diagnostics and keep safe defaults for the affected role.

