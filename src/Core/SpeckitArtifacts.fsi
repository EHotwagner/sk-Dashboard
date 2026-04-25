namespace SkDashboard.Core

module SpeckitArtifacts =
    val resolveRepositoryRoot : string -> string
    val resolveUserConfigPath : unit -> string
    val discoverFeatureDirectories : string -> Feature list
    val summarizeFeatureStatus : string -> FeatureStatus
    val loadPlan : string -> Plan
    val parseUserStories : string -> UserStory list * Diagnostic list
    val parseTasks : string -> SpeckitTask list * Diagnostic list
    val loadSnapshot : string -> DashboardSnapshot
