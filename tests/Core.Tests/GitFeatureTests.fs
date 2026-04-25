module SkDashboard.Core.Tests.GitFeatureTests

open System.IO
open Expecto
open SkDashboard.Core

let git repositoryRoot arguments =
    let result = GitFeatures.runProcess "git" arguments repositoryRoot
    Expect.equal result.ExitCode 0 (sprintf "git %A failed: %s" arguments result.StandardError)
    result

let initRepo () =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-git-").FullName
    git root [ "init" ] |> ignore
    git root [ "config"; "user.email"; "test@example.invalid" ] |> ignore
    git root [ "config"; "user.name"; "sk-dashboard tests" ] |> ignore
    File.WriteAllText(Path.Combine(root, "README.md"), "root")
    git root [ "add"; "." ] |> ignore
    git root [ "commit"; "-m"; "initial" ] |> ignore
    root

let createBranch repositoryRoot branchName (content: string) =
    git repositoryRoot [ "checkout"; "-b"; branchName ] |> ignore
    Directory.CreateDirectory(Path.Combine(repositoryRoot, "specs", branchName)) |> ignore
    File.WriteAllText(Path.Combine(repositoryRoot, "specs", branchName, "spec.md"), content)
    git repositoryRoot [ "add"; "." ] |> ignore
    git repositoryRoot [ "commit"; "-m"; "add " + branchName ] |> ignore

[<Tests>]
let gitFeatureTests =
    testList "Git features" [
        test "parseFeatureBranch_accepts numeric speckit branch" {
            let feature = GitFeatures.parseFeatureBranch "001-speckit-tui-dashboard"
            Expect.isSome feature "Numeric branch should be recognized."
        }

        test "selectLatestFeature_uses parsed order" {
            let features =
                [ "001-a"; "010-b"; "002-c" ]
                |> List.choose GitFeatures.parseFeatureBranch

            let latest = GitFeatures.selectLatestFeature features
            Expect.equal (latest |> Option.map _.Id) (Some "010-b") "Highest numeric prefix wins."
        }

        test "selectLatestFeature_supports timestamp ordering" {
            let features =
                [ "20260424091500-old"; "20260425103000-new"; "20260425000100-middle" ]
                |> List.choose GitFeatures.parseFeatureBranch

            let latest = GitFeatures.selectLatestFeature features
            Expect.equal (latest |> Option.map _.Id) (Some "20260425103000-new") "Newest timestamp prefix wins."
        }

        test "selectLatestFeature_supports fallback ordering" {
            let features =
                [ "feature/alpha"; "feature/zeta"; "feature/beta" ]
                |> List.choose GitFeatures.parseFeatureBranch

            let latest = GitFeatures.selectLatestFeature features
            Expect.equal (latest |> Option.map _.Id) (Some "zeta") "Fallback branches are deterministic."
        }

        test "selectLatestFeature_tie_breaks deterministically" {
            let features =
                [ "010-beta"; "010-alpha"; "010-zeta" ]
                |> List.choose GitFeatures.parseFeatureBranch

            let latest = GitFeatures.selectLatestFeature features
            Expect.equal (latest |> Option.map _.DisplayName) (Some "010-zeta") "Display name tie-break is deterministic."
        }

        test "listFeatureBranches_reads ten feature branches from git" {
            let root = initRepo ()

            for index in 1 .. 10 do
                createBranch root (sprintf "%03i-feature" index) "# Spec"

            let branches = GitFeatures.listFeatureBranches root
            Expect.isGreaterThanOrEqual branches.Length 10 "At least 10 feature branches are discovered."
            Expect.equal (GitFeatures.selectLatestFeature branches |> Option.map _.Id) (Some "010-feature") "Latest branch is selected from repo branches."
        }

        test "checkoutBranch_reports local-state checkout failure" {
            let root = initRepo ()
            let baseBranch = (git root [ "branch"; "--show-current" ]).StandardOutput.Trim()
            createBranch root "001-old" "old"
            File.WriteAllText(Path.Combine(root, "conflict.txt"), "old")
            git root [ "add"; "." ] |> ignore
            git root [ "commit"; "-m"; "old conflict" ] |> ignore

            git root [ "checkout"; baseBranch ] |> ignore
            createBranch root "002-new" "new"
            File.WriteAllText(Path.Combine(root, "conflict.txt"), "new")
            git root [ "add"; "." ] |> ignore
            git root [ "commit"; "-m"; "new conflict" ] |> ignore

            git root [ "checkout"; "001-old" ] |> ignore
            File.WriteAllText(Path.Combine(root, "conflict.txt"), "local edit")
            let checkout = GitFeatures.checkoutBranch root "002-new"
            Expect.isTrue (match checkout with Failed _ -> true | _ -> false) "Dirty tracked file blocks checkout."
        }

        test "runProcess_drains large redirected output" {
            let result =
                GitFeatures.runProcess
                    "bash"
                    [ "-c"; "for i in $(seq 1 20000); do echo stdout-$i; echo stderr-$i 1>&2; done" ]
                    "."

            Expect.equal result.ExitCode 0 "Command exits normally."
            Expect.stringContains result.StandardOutput "stdout-20000" "Large stdout is drained."
            Expect.stringContains result.StandardError "stderr-20000" "Large stderr is drained."
        }
    ]
