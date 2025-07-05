module LithumIron.App

open System
open System.IO
open System.Threading
open FSharp.Collections

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

let killBeyond (Cell(mx, my)) (board: Cell Set) =
    board |> Set.filter (fun (Cell(x, y)) -> x <= mx && y <= my)

[<EntryPoint>]
let main _args =
    let boundary = (66, 23) |> Cell |> killBeyond
    let advanceWithBoundary = advance >> boundary
    let mutable board =
        "board.lfe"
        |> File.ReadAllLines
        |> Array.indexed
        |> Seq.collect (fun (y, line) -> line |> Seq.mapi (fun x c -> x, y, c = '#'))
        |> Seq.choose (fun (x, y, alive) -> if alive then (x, y) |> Cell |> Some else None)
        |> Set.ofSeq

    Console.CancelKeyPress.Add(fun _args -> Console.CursorVisible <- true)
    Console.CursorVisible <- false

    while not <| Set.isEmpty board do
        Console.Write "\x1bc"

        for y in 0..20 do
            for x in 0..63 do
                if (x, y) |> Cell |> active board then '\u25cf' else ' '
                |> Console.Write

            Console.WriteLine()

        board <- advanceWithBoundary board
        Thread.Sleep 83

    Console.CursorVisible <- true

    0
