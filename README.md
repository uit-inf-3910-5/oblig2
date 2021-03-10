# Oblig 2: Parsing CSV

In this exercise you will use _parser combinators_ to write
elegant and powerful parsers. To start things off, you will write a
simple CSV parser. We will then extend it with a simple type system, so that the
data the parser returns is correctly typed. Finally we will add a few unary and
binary operators (abs, max, min, add) to the parser, so that it can perform
computation while parsing.

You will be given a stub project to work on. The project has two main parts: a
parser combinator library and a stub csv parser. The parser combinator library
is largely compatible with the production level FParsec library, but is much
more simple minded (slow and incomplete). It is provided for reference, but you
should probably use FParsec for the actual project, since the error reporting is
much better, making debugging easier.

## Prerequisistes

It is essential to watch Scott Wlaschin's excellent presentation on _parser
combinators_ before you begin. It's also helpful to go through his blog posts on
the topic:

1. [Video](https://goo.gl/Cxa7NR)
2. [Blog posts](https://fsharpforfunandprofit.com/series/understanding-parser-combinators.html)
3. Study the provided parser combinator library in Parsec.fs. It's very similar
   to Scott's library.

## Exercises

1. Using the stub in ``src/Oblig2.fs``, write a parser to parse the contents
   of ``ex1.csv`` as strings.
2. Modify the parser from 1. to return values with the correct type, e.g.
   ints as ints, floats as floats, etc.

   Hint: The Parser is a functor. Use ``map`` (or ``|>>``) to convert the parsed
   string to the right type of value. You can transform the output from the
   parser to a more useful structure (record type) using a _smart constructor_
   and the _applicative_ combinators ``<!>`` and ``<*>``.
3. Improve the parser to first parse a header with the types of the columns,
   then parse each line according to the type specification.

   Hint: Using ``|>>`` you can change the parser from e.g ``Parser<string>`` to
   ``Parser<Parser<string>>``. You can thus parse a type specification (e.g. "int"),
   and return a new parser of ints. The returned parser can be run to parse
   consequent lines of input, containing the actual csv data.
4. Modify the Parser from 2. to include the following typed operators:
   ``abs, add, max, min``. The type is specified id the following manner: ``op :
   type``, e.g. "add : int" (see ``ex3.csv``).
5. Implement a computatin expression for the parser combinator, and implement 2.
   (or 3.) in "monadic" style using the computaton expression syntax.

## Building the project

### Prerequisites

* Install .NET 5.0

### Building and running

```sh
cd src
dotnet build
dotnet run ex1.csv
```

## Good luck

This exericse combines almost everything you have learned so far: Functions,
higher-order functions, partial application, functors, applicative funtors,
monads and custom operators. It's a mouthful, but you can do it. Talk to each
other, and don't be afraid to ask questions on Canvas. I will answer to the best
of by ability.

_May the foo be with you!_
