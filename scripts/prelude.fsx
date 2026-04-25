// prelude.fsx - load the built Core public surface for FSI checks.

#r "../src/Core/bin/Debug/net10.0/Core.dll"

open SkDashboard.Core

printfn "prelude: SkDashboard.Core loaded. Try: SkDashboard.Core.SpeckitArtifacts.loadSnapshot \".\""
