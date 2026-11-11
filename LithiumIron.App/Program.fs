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
        | Sloth -> 2L
        | Turtle -> 4L
        | Human -> 8L
        | Zebra -> 16L
        | Cheetah -> 32L

[<RequireQualifiedAccess>]
type CliArguments =
    | [<MainCommand; ExactlyOnce; Last>] Board of filename: string
    | [<AltCommandLine("-k")>] Keep
    | [<AltCommandLine("-s")>] Speed of GameSpeed

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Board _ -> "Filename of the desired initial board state"
            | Keep -> "Never destroy active cells outside of the displayed board"
            | Speed _ -> "How fast the game will render each frame [default: human]"

type Cell = Cell of int * int

type ConsoleApplicationContext(stream: Stream) =
    let writer =
        let encoding = Text.Encoding.UTF8
        let bufferSize = 32768
        let w = new StreamWriter(stream, encoding, bufferSize)
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

let neighbors (Cell(x, y)) : Cell array =
    [|
        Cell(x + 1, y)
        Cell(x - 1, y)
        Cell(x, y + 1)
        Cell(x, y - 1)
        Cell(x + 1, y + 1)
        Cell(x + 1, y - 1)
        Cell(x - 1, y + 1)
        Cell(x - 1, y - 1)
    |]

let active (board: Cell Set) position = Set.contains position board

let advance (board: Cell Set) : Cell Set =
    let counts = Collections.Generic.Dictionary<Cell, int>(board.Count * 8)
    let mutable nextBoard = Set.empty

    for cell in board do
        for n in neighbors cell do
            counts[n] <-
                match counts.TryGetValue n with
                | true, count -> count + 1
                | false, _ -> 1

    for KeyValue(cell, count) in counts do
        if count = 3 || count = 2 && board.Contains cell then
            nextBoard <- Set.add cell nextBoard

    nextBoard

let readBoardFile (windowSize: int * int) filename =
    let columns, rows = windowSize
    let mutable minX, maxX = Int32.MaxValue, 0
    let mutable minY, maxY = Int32.MaxValue, 0
    let mutable cells = []
    let lines = File.ReadAllLines filename

    for y = 0 to lines.Length - 1 do
        let line = lines[y]

        for x = 0 to line.Length - 1 do
            if line[x] = '#' then
                cells <- (x, y) :: cells

                if x < minX then
                    minX <- x

                if x > maxX then
                    maxX <- x

                if y < minY then
                    minY <- y

                if y > maxY then
                    maxY <- y

    if List.isEmpty cells then
        Set.empty
    else
        let width = maxX - minX + 1
        let height = maxY - minY + 1
        let offsetX = (columns - width) / 2 - minX
        let offsetY = (rows - height) / 2 - minY

        cells |> List.map (fun (x, y) -> Cell(x + offsetX, y + offsetY)) |> Set.ofList

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

    let windowSize =
        try
            Console.WindowWidth, Console.WindowHeight * 2
        with _ ->
            80, 48

    let mutable board = options.GetResult CliArguments.Board |> readBoardFile windowSize
    let speed = options.TryGetResult CliArguments.Speed |> Option.defaultValue Human
    let frameBudgetMs = 1000L / speed.FrameRate

    let advanceWithBoundary =
        if options.Contains CliArguments.Keep then
            // Keep and calculate all cells even if beyond the rendered board
            advance
        else
            // Kill cells past the given bottom-right boundary
            let boundary = windowSize |> Cell |> killBeyond
            advance >> boundary

    let lastColumn, lastRow =
        let width, height = windowSize
        width - 1, height - 1

    let stopwatch = Diagnostics.Stopwatch()

    use app = new ConsoleApplicationContext(Console.OpenStandardOutput())

    Console.CancelKeyPress.Add(fun _ ->
        do (app :> IDisposable).Dispose()
        do board <- Set.empty)

    while not <| Set.isEmpty board do
        stopwatch.Restart()
        renderFrame lastRow lastColumn board app.Writer

        board <-
            match tryReadKey () with
            | Some Exit -> Set.empty
            | _ -> advanceWithBoundary board

        let millisecondsTimeout = int (frameBudgetMs - stopwatch.ElapsedMilliseconds)

        if millisecondsTimeout > 0 then
            Thread.Sleep millisecondsTimeout

    0
