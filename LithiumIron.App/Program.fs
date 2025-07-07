module LithumIron.App

open System
open System.IO
open System.Threading
open Argu
open FSharp.Collections

type GameSpeed =
    | Sloth
    | Turtle
    | Human
    | Zebra
    | Cheetah

    member this.FrameRate =
        match this with
        | Sloth -> 2
        | Turtle -> 4
        | Human -> 8
        | Zebra -> 12
        | Cheetah -> 16

[<RequireQualifiedAccess>]
type CliArguments =
    | [<MainCommand; ExactlyOnce; Last>] Board of filename: string
    | [<AltCommandLine("-k")>] Keep
    | [<AltCommandLine("-a")>] Adjust_Board of x: int * y: int
    | [<AltCommandLine("-c")>] Columns of int
    | [<AltCommandLine("-r")>] Rows of int
    | [<AltCommandLine("-s")>] Speed of GameSpeed

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Board _ -> "Filename of the desired initial board state"
            | Keep -> "Never destroy active cells outside of the displayed board"
            | Adjust_Board _ -> "Adjust every coordinate of the input board"
            | Rows _ -> "Specify number of board rows"
            | Columns _ -> "Specify number of board columns"
            | Speed _ -> "How fast the game will render each frame [default: human]"

type Cell = Cell of int * int

let neighbors (Cell(x, y)) : Cell seq =
    seq {
        x + 1, y
        x - 1, y
        x, y + 1
        x, y - 1
        x + 1, y + 1
        x + 1, y - 1
        x - 1, y + 1
        x - 1, y - 1
    }
    |> Seq.map Cell

let active (board: Cell Set) position = Set.contains position board

let advance (board: Cell Set) : Cell Set =
    let valueOf (position: Cell) : int = if active board position then 1 else 0

    board
    |> Seq.collect neighbors
    |> Seq.append board
    |> Set.ofSeq
    |> Set.filter (fun (cell) ->
        let count = cell |> neighbors |> Seq.sumBy valueOf
        let alive = count = 3 || count = 2 && active board cell

        alive)

let readBoardFile (adjustments: int * int) filename =
    let (ax, ay) = adjustments

    filename
    |> File.ReadAllLines
    |> Array.indexed
    |> Seq.collect (fun (y, line) -> line |> Seq.mapi (fun x c -> x, y, c = '#'))
    |> Seq.choose (fun (x, y, alive) -> if alive then (x + ax, y + ay) |> Cell |> Some else None)
    |> Set.ofSeq

let minColumns (board: Cell Set) (additional: int) =
    board |> Seq.map (fun (Cell(x, _)) -> x) |> Seq.max |> (+) additional

let minRows (board: Cell Set) (additional: int) =
    board |> Seq.map (fun (Cell(_, y)) -> y) |> Seq.max |> (+) additional

let killBeyond (Cell(mx, my)) (board: Cell Set) =
    board |> Set.filter (fun (Cell(x, y)) -> x <= mx && y <= my)

let errorColors =
    function
    | ErrorCode.HelpText -> None
    | _ -> Some ConsoleColor.Red

let renderFrame lastRow lastColumn board =
    Console.Write "\x1bc"

    for y in 0..lastRow do
        for x in 0..lastColumn do
            if (x, y) |> Cell |> active board then '\u25cf' else ' '
            |> Console.Write

        if y <> lastRow then
            Console.WriteLine()

[<EntryPoint>]
let main argv =
    let parser =
        ArgumentParser.Create<CliArguments>(programName = "life", errorHandler = ProcessExiter(colorizer = errorColors))

    let options = parser.ParseCommandLine argv

    let adjustments =
        options.TryGetResult(CliArguments.Adjust_Board) |> Option.defaultValue (2, 2)

    let mutable board =
        options.GetResult(CliArguments.Board) |> readBoardFile adjustments

    let columnCount =
        options.TryGetResult CliArguments.Columns
        |> Option.defaultWith (fun () -> adjustments |> fst |> minColumns board)

    let rowCount =
        options.TryGetResult CliArguments.Rows
        |> Option.defaultWith (fun () -> adjustments |> snd |> minRows board)

    let speed = options.TryGetResult CliArguments.Speed |> Option.defaultValue Human
    let millisecondsTimeout = 1000 / speed.FrameRate

    let advanceWithBoundary =
        if options.Contains CliArguments.Keep then
            // Keep and calculate all cells even if beyond the rendered board
            advance
        else
            // Kill cells past the given bottom-right boundary
            let boundary = (columnCount + 2, rowCount + 2) |> Cell |> killBeyond
            advance >> boundary

    let lastRow = rowCount - 1
    let lastColumn = columnCount - 1

    Console.CancelKeyPress.Add(fun _args -> Console.CursorVisible <- true)
    Console.CursorVisible <- false

    renderFrame lastRow lastColumn board
    while not <| Set.isEmpty board do
        board <- advanceWithBoundary board
        Thread.Sleep millisecondsTimeout
        renderFrame lastRow lastColumn board

    Console.CursorVisible <- true

    0
