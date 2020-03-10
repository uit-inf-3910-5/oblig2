namespace Oblig2

open System

module Main =

    [<EntryPoint>]
    let main argv =
        try
            let lines = IO.File.ReadAllLines argv.[0]
            Parser.parseCsv lines
            |> printfn "%A"
        with 
            ex -> printfn "%s" ex.Message
        0 // return an integer exit code
