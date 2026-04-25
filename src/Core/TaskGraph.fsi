namespace SkDashboard.Core

module TaskGraphBuilder =
    val build : string option -> SpeckitTask list -> Diagnostic list -> TaskGraph
