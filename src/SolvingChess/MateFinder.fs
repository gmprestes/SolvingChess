﻿module MateFinder

open Position
open Moves
open Check

type Record = { 
    M: Move; 
    P: Position; 
}
with 
    member x.Ms = (moves x.P) |> Seq.toArray

    member x.hasResponses = x.Ms.Length > 0
    member x.check = (isCheck x.P)
    member x.withNoKingMoves = not (Array.exists (fun move -> move.Piece = King) x.Ms )
    
    member x.checkMate = x.Ms.Length = 0 && isCheckMate x.P

    member x.score = 
        -x.Ms.Length + (if x.check then 1 else 0) + (if x.withNoKingMoves then 1 else 0)

    override x.ToString() = x.M.ToString()

let mutable numberOfCalls = 0

open System.Collections.Generic

let rec findMate position depth maxdepth = 
    numberOfCalls <- if depth = 0 then 0 else numberOfCalls + 1
       
    if depth < maxdepth then

        let continuations = 
            moves position
            |> Seq.map(fun(move) -> {M=move; P=applyMove move position })
            |> Seq.sortBy (fun record -> record.Ms.Length) 
            |> Seq.toArray

        let mate = 
            continuations
            |> Array.takeWhile (fun record -> not record.hasResponses)
            |> Array.tryFind   (fun record -> record.checkMate)

        match mate with
        | Some(record) ->  
            printfn "%s%s++" (String.replicate (depth + 1) " ") (record.M.ToString())
            Some ([| record.M |])
        | None ->
            let alternatives = 
                continuations 
                |> Array.skipWhile (fun alternative -> not alternative.hasResponses)

            match position.SideToMove with
            | Black ->
                let rec explore (enumerator:IEnumerator<Move[] option>): Move[] option =
                    if enumerator.MoveNext() 
                    then
                        let c1 = enumerator.Current
                        if c1 = None 
                        then None
                        else
                            let c2 = explore enumerator
                            match c1, c2 with
                            | None, _ -> None
                            | _, None -> None
                            | Some(a), Some(b) -> Some(if a.Length > b.Length then a else b)
                    else
                        Some(Array.empty<Move>)
                   
                let future = 
                    alternatives
                    |> Seq.map (fun alternative -> 
                                    printfn "%s%s" (String.replicate (depth + 1) " ") (alternative.ToString())
                                    let line = findMate alternative.P (depth + 1) maxdepth
                                    match line with 
                                    | Some(x) -> Some(Array.append [| alternative.M |] x)
                                    | None -> None
                                  )

                explore (future.GetEnumerator())

            | White -> 
                let rec explore (enumerator:IEnumerator<Record>) (maxdepth) : Move[] option =
                    if enumerator.MoveNext() 
                    then
                        let move = enumerator.Current.M
                        printfn "%s%s" (String.replicate (depth + 1) " ") (move.ToString())
                        let line1 = findMate enumerator.Current.P (depth + 1) maxdepth
                        let line2 = explore enumerator (if line1 = None then maxdepth else depth + line1.Value.Length)
                        match line1, line2 with 
                        | Some(x), None -> Some(Array.append [| move |] x)
                        | None, Some(x) -> Some(x)
                        | Some(x), Some(y) -> Some (if x.Length < y.Length then (Array.append [| move |] x) else y)
                        | None, None -> None
                    else
                        None

                let future = 
                    alternatives
                    |> Array.sortByDescending (fun alternative -> alternative.score)
                    |> Seq.where(fun alternative -> alternative.Ms.Length <= 3)
//                    |> Seq.where(fun alternative -> 
//                                    alternative.Ms.Length < 3 
//                                    || alternative.check 
//                                    //|| (alternative.M.Piece <> Pawn && alternative.withNoKingMoves)
//                                 )

                explore (future.GetEnumerator()) maxdepth
    else
        None
            
