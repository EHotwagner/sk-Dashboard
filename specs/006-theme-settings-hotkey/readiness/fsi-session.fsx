#r "../../../src/Core/bin/Debug/net10.0/Core.dll"

open SkDashboard.Core

let appThemeIds = Domain.builtInAppThemes |> List.map _.Id
let markdownThemeIds = Domain.builtInMarkdownThemes |> List.map _.Id
let checklistCommand = Hotkeys.commandId ChecklistOpen, Hotkeys.commandLabel ChecklistOpen
let checklistBinding =
    Hotkeys.defaultBindings
    |> List.find (fun binding -> binding.Command = ChecklistOpen)
    |> fun binding -> binding.KeySequence

printfn "app themes: %A" appThemeIds
printfn "markdown themes: %A" markdownThemeIds
printfn "checklist command: %A" checklistCommand
printfn "checklist binding: %s" checklistBinding
