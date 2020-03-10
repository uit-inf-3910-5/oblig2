namespace Oblig2

open System

open FParsec

module Parser =

    [<AutoOpen>]
    module Helpers =
        // constant function. handy alternative to a lambda ignoring it's args
        let inline constf x _ = x

        // applicative functor apply
        let apply fP xP =
            fP >>= (fun f -> xP >>= (preturn << f))

        let (<*>) = apply

        // (::) cannot be used as a prefix function. don't ask why...
        let cons head tail = head :: tail

        // monoid for list parser
        let pempty = preturn []

        // convert a list of parsers to a parser of list
        let sequence pl =
            List.foldBack (fun x a -> pipe2 x a cons) pl pempty

        let pjoin (x, xs) = x :: xs |> sequence

    // natural transformation ParserResult ~> Result
    let toResult =
        function
        | Success (p, _, _) -> Result.Ok p
        | Failure _ as err -> Result.Error err

    // to be implemented
    let parseHeader = preturn (preturn "")

    let parseCsv (lines : string array) =
        run parseHeader lines.[0]
        |> toResult
        |> Result.map (fun p -> Array.map (run p) (Array.tail lines))

    // FParsec is a bit tricky with types
    let dummyToMakeTypeSystemHappy : ParserResult<unit list, unit> = run pempty ""
