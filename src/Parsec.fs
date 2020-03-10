namespace Oblig2

open System

type Parser<'a> = Parser of (string -> Result<'a * string, string>)

module Parsec =
    let run parser input = 
        // unwrap parser to get inner function
        let (Parser innerFn) = parser 
        // call inner function with input
        innerFn input

    let orElse parser1 parser2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run parser1 input

            // test the result for Error/Ok
            match result1 with
            | Ok result -> 
                // if success, return the original result
                result1

            | Error err -> 
                // if failed, run parser2 with the input
                let result2 = run parser2 input

                // return parser2's result
                result2 

        // return the inner function
        Parser innerFn 

    let (<|>) = orElse

    let bind f p =
        let innerFn input =
            let result1 = run p input
            match result1 with
            | Error err ->
                // return error from parser1
                Error err
            | Ok (value1,remainingInput) ->
                // apply f to get a new parser
                let p2 = f value1
                // run parser with remaining input
                run p2 remainingInput
        Parser innerFn

    let (>>=) p f = bind f p

    let preturn x =
        let innerFn input =
            // ignore the input and return x
            Ok (x,input )
        // return the inner function
        Parser innerFn
    
    // parser monoid
    let pempty = preturn []

    let map f = bind (f >> preturn)

    let (<!>) = map

    let (|>>) x f = map f x

    let andThen p1 p2 =
        p1 >>= (fun p1Result ->
        p2 >>= (fun p2Result ->
            preturn (p1Result, p2Result) ))
    let (.>>.) = andThen

    let apply fP xP =
        fP >>= (fun f ->
        xP >>= (preturn << f))

    let (<*>) = apply

    let lift2 f xP yP = preturn f <*> xP <*> yP

    let pipe2 xP yP f = preturn f <*> xP <*> yP

    let rec sequence parserList =
        // define the "cons" function, which is a two parameter function
        let cons head tail = head::tail
        // lift it to Parser World
        let consP = lift2 cons
        // process the list of parsers recursively
        match parserList with
        | [] -> pempty
        | head::tail -> consP head (sequence tail)

    let (.>>) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the first value
        |> map fst

    /// Keep only the result of the right side parser
    let (>>.) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the second value
        |> map snd

    let add = lift2 (+)

    let choice listOfParsers = 
        List.reduce (<|>) listOfParsers 

    let pchar charToMatch =
        // define a nested inner function
        let innerFn str =
            if String.IsNullOrEmpty(str) then
                Error "No more input"
            else
                let first = str.[0] 
                if first = charToMatch then
                    let remaining = str.[1..]
                    Ok (charToMatch,remaining)
                else
                    let msg = sprintf "Expecting '%c'. Got '%c'" charToMatch first
                    Error msg
        // return the inner function
        Parser innerFn 

    let anyOf listOfChars = 
        listOfChars
        |> List.map pchar // convert into parsers
        |> choice

    let parseLowercase = anyOf ['a'..'z']

    let parseDigit = anyOf ['0'..'9']

    /// "bind" takes a parser-producing function f, and a parser p
    /// and passes the output of p into f, to create a new parser

    let rec parseZeroOrMore parser input =
        // run parser with the input
        let firstResult = run parser input
        // test the result for Error/Ok
        match firstResult with
        | Error err ->
            // if parse fails, return empty list
            ([],input)
        | Ok (firstValue,inputAfterFirstParse) ->
            // if parse succeeds, call recursively
            // to get the subsequent values
            let (subsequentValues,remainingInput) =
                parseZeroOrMore parser inputAfterFirstParse
            let values = firstValue::subsequentValues
            (values,remainingInput)

    /// match zero or more occurences of the specified parser
    let many parser =

        let rec innerFn input =
            // parse the input -- wrap in Ok as it always succeeds
            Ok (parseZeroOrMore parser input)

        Parser innerFn


    let many1 p =
        p      >>= (fun head ->
        many p >>= (fun tail ->
            preturn (head :: tail) ))

    let parseThreeDigitsAsStr =
        (parseDigit .>>. parseDigit .>>. parseDigit)
        |>> fun ((c1, c2), c3) -> String [| c1; c2; c3 |]

    let parseThreeDigitsAsInt = int <!> parseThreeDigitsAsStr

    let startsWith (str:string) (prefix : string) =
        str.StartsWith prefix

    let startsWithP = lift2 startsWith

    /// Helper to create a string from a list of chars
    let charListToStr charList = String(List.toArray charList)

    // match a specific string
    let pstring str =
        str
        // convert to list of char
        |> List.ofSeq
        // map each char to a pchar
        |> List.map pchar
        // convert to Parser<char list>
        |> sequence
        // convert Parser<char list> to Parser<string>
        |> map charListToStr


    let opt p =
        let some = p |>> Some
        let none = preturn None
        some <|> none

        // define parser for one digit
    
    let digit = anyOf ['0'..'9']

    // define parser for one or more digits
    let digits = many1 digit

    let asciiLetter = anyOf ['!'..'~'] <|> digit

    let pint32 =
        let resultToInt (sign, charList) =
            let i = String(List.toArray charList) |> int
            match sign with
            | Some ch -> -i  // negate the int
            | None -> i

        opt (pchar '-') .>>. digits
        |>> resultToInt

    let pfloat=
        let resultToFloat (sign, charList) =
            let i = String(List.toArray charList) |> float
            match sign with
            | Some ch -> -i  // negate 
            | None -> i
        opt (pchar '-') .>>. digits .>>. (pchar '.' >>. digits)
        |>> fun ((sign, x), y)  -> sign, List.append x ('.' :: y)
        |>> resultToFloat

    let whitespace = anyOf [' '; '\t'; '\n']

    let spaces1 = many1 whitespace

    let spaces = many whitespace

    let between p1 p3 p2 = p1 >>. p2 .>> p3

    let sepBy1 p sep =
        let sepThenP = sep >>. p
        p .>>. many sepThenP
        |>> fun (p, pList) -> p::pList

    let sepBy p sep =
        sepBy1 p sep <|> pempty
