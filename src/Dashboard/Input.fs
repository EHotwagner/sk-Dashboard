namespace SkDashboard.Dashboard

open System
open System.Threading
open SkDashboard.Core

type DashboardEvent =
    | RefreshRequested
    | QuitRequested
    | NoOp

module Input =
    let keySequenceFromConsoleKey (key: ConsoleKeyInfo) =
        match key.Key with
        | ConsoleKey.UpArrow -> "up"
        | ConsoleKey.DownArrow -> "down"
        | ConsoleKey.LeftArrow when key.Modifiers.HasFlag ConsoleModifiers.Shift -> "shift+left"
        | ConsoleKey.RightArrow when key.Modifiers.HasFlag ConsoleModifiers.Shift -> "shift+right"
        | ConsoleKey.LeftArrow -> "left"
        | ConsoleKey.RightArrow -> "right"
        | ConsoleKey.Enter -> "enter"
        | ConsoleKey.Escape -> "esc"
        | ConsoleKey.Tab when key.Modifiers.HasFlag ConsoleModifiers.Shift -> "shift+tab"
        | ConsoleKey.Tab -> "tab"
        | _ ->
            if key.KeyChar = Char.MinValue then
                ""
            else
                string key.KeyChar

    let readKeySequence () =
        let first = Console.ReadKey(true)

        if first.Key <> ConsoleKey.Escape then
            keySequenceFromConsoleKey first
        else
            // Some terminals deliver arrows as raw CSI escape bytes instead
            // of ConsoleKey values.
            Thread.Sleep 10

            if not Console.KeyAvailable then
                "esc"
            else
                let chars = ResizeArray<char>()

                while Console.KeyAvailable && chars.Count < 8 do
                    chars.Add(Console.ReadKey(true).KeyChar)

                match String(chars.ToArray()) with
                | "[A" -> "up"
                | "[B" -> "down"
                | "[D" -> "left"
                | "[C" -> "right"
                | "[1;2D"
                | "[D;2" -> "shift+left"
                | "[1;2C"
                | "[C;2" -> "shift+right"
                | _ -> "esc"

    let commandForKeyWithBindings bindings key =
        bindings
        |> List.tryFind (fun binding -> binding.KeySequence = key)
        |> Option.map _.Command

    let commandForKey key =
        commandForKeyWithBindings Hotkeys.defaultBindings key

    let eventForCommand command =
        match command with
        | Quit -> QuitRequested
        | Refresh -> RefreshRequested
        | _ -> NoOp
