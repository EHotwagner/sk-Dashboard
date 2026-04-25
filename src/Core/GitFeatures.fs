namespace SkDashboard.Core

open System
open System.Diagnostics
open System.Text.RegularExpressions

type ProcessResult =
    { ExitCode: int
      StandardOutput: string
      StandardError: string }

module GitFeatures =
  let runProcess (fileName: string) (arguments: string list) (workingDirectory: string) =
    let startInfo = ProcessStartInfo()
    startInfo.FileName <- fileName
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true

    for argument in arguments do
        startInfo.ArgumentList.Add argument

    use proc =
        match Process.Start startInfo with
        | null -> failwith ("Unable to start process: " + fileName)
        | started -> started

    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()

    { ExitCode = proc.ExitCode
      StandardOutput = stdout.GetAwaiter().GetResult()
      StandardError = stderr.GetAwaiter().GetResult() }

  let parseOrderKey (branchName: string) =
    let clean =
        branchName
            .Replace("origin/", "")
            .Replace("feature/", "")
            .Trim()

    let prefix = clean.Split('-', StringSplitOptions.RemoveEmptyEntries) |> Array.tryHead

    match prefix with
    | Some value ->
        match Int32.TryParse value with
        | true, number -> Numeric number
        | false, _ when Regex.IsMatch(value, "^\d{8,14}$") -> Timestamp value
        | _ -> Fallback clean
    | None -> Fallback clean

  let parseFeatureBranch (branchName: string) =
    let clean = branchName.Trim().TrimStart('*').Trim()

    if String.IsNullOrWhiteSpace clean then
        None
    else
        let display = clean.Replace("origin/", "")
        let isFeature =
            Regex.IsMatch(display, @"^(\d+|feature/|[0-9]{8,14}-)")
            || display.Contains("-")

        if not isFeature then
            None
        else
            Some
                { Id = display.Replace("feature/", "")
                  BranchName = Some display
                  DisplayName = display
                  OrderKey = parseOrderKey display
                  IsSelected = false
                  ArtifactRoot = None
                  CheckoutState = NotAttempted
                  Status = None }

  let listFeatureBranches repositoryRoot =
    let result = runProcess "git" [ "branch"; "--format=%(refname:short)" ] repositoryRoot

    if result.ExitCode <> 0 then
        []
    else
        result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        |> Array.choose parseFeatureBranch
        |> Array.toList

  let orderWeight feature =
    match feature.OrderKey with
    | Numeric value -> 2, value.ToString("D20"), feature.DisplayName
    | Timestamp value -> 1, value, feature.DisplayName
    | Fallback value -> 0, value, feature.DisplayName

  let selectLatestFeature features =
    features
    |> List.sortBy orderWeight
    |> List.tryLast

  let currentBranch repositoryRoot =
    let result = runProcess "git" [ "branch"; "--show-current" ] repositoryRoot

    if result.ExitCode = 0 then
        let branch = result.StandardOutput.Trim()
        if String.IsNullOrWhiteSpace branch then None else Some branch
    else
        None

  let checkoutBranch repositoryRoot branchName =
    let result = runProcess "git" [ "checkout"; branchName ] repositoryRoot

    if result.ExitCode = 0 then
        CheckedOut
    else
        Failed(result.StandardError.Trim())
