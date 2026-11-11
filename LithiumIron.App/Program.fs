module LithumIron.App

open System
open System.IO
open System.Threading
open Argu
open FSharp.Collections

[<Literal>]
let ClearScreen = "\x1bc"

[<Literal>]
let EnterAlternateScreen = "\x1b[?1049h"

[<Literal>]
let ExitAlternateScreen = "\x1b[?1049l"

[<Literal>]
let BlockUpper = '\u2580'

[<Literal>]
let BlockLower = '\u2584'

[<Literal>]
let BlockFull = '\u2588'

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
        | Zebra -> 16
        | Cheetah -> 32

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

type ConsoleApplicationContext(stream: Stream) =
    let writer =
        let w = new StreamWriter(stream)
        w.AutoFlush <- false
        w

    do Console.CursorVisible <- false
    do Console.Write EnterAlternateScreen

    interface IDisposable with
        member _.Dispose() : unit =
            do writer.Dispose()
            do Console.Write ClearScreen
            do Console.Write ExitAlternateScreen
            do Console.CursorVisible <- true

    member _.Writer = writer

let tryReadKey () : ConsoleKeyInfo option =
    if Console.KeyAvailable then
        Console.ReadKey true |> Some
    else
        None

let (|Exit|_|) (keyInfo: ConsoleKeyInfo) =
    if keyInfo.Modifiers = ConsoleModifiers.Control && keyInfo.Key = ConsoleKey.Q then
        Some()
    else
        None

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

let renderFrame lastRow lastColumn board (out: StreamWriter) =
    out.Write ClearScreen

    let lineBuffer () = Array.create (lastColumn + 1) ' '

    for y in 0..2..lastRow do
        let yUpper, yLower = y, if y > lastRow then None else y + 1 |> Some
        let line = lineBuffer ()

        if y > 0 then
            do out.WriteLine()

        for x in 0..lastColumn do
            let cellUpper = (x, yUpper) |> Cell |> active board

            let cellLower =
                yLower
                |> Option.map (fun y -> (x, y) |> Cell |> active board)
                |> Option.defaultValue false

            match cellUpper, cellLower with
            | true, true -> line[x] <- BlockFull
            | true, false -> line[x] <- BlockUpper
            | false, true -> line[x] <- BlockLower
            | false, false -> ()

        out.Write line

    out.Flush()

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

    use app = new ConsoleApplicationContext(Console.OpenStandardOutput())

    Console.CancelKeyPress.Add(fun _ ->
        do (app :> IDisposable).Dispose()
        do board <- Set.empty)

    while not <| Set.isEmpty board do
        renderFrame lastRow lastColumn board app.Writer

        board <-
            match tryReadKey () with
            | Some Exit -> Set.empty
            | _ -> advanceWithBoundary board

        Thread.Sleep millisecondsTimeout

    0
