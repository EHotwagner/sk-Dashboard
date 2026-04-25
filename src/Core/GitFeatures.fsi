namespace SkDashboard.Core

type ProcessResult =
    { ExitCode: int
      StandardOutput: string
      StandardError: string }

module GitFeatures =
    val runProcess : string -> string list -> string -> ProcessResult
    val parseFeatureBranch : string -> Feature option
    val listFeatureBranches : string -> Feature list
    val selectLatestFeature : Feature list -> Feature option
    val currentBranch : string -> string option
    val checkoutBranch : string -> string -> CheckoutState
