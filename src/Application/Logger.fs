module Application.Logger

open System
open System.IO
open System.Text

type LoggingLevel = 
    | Error = 0
    | Warning = 1
    | Info = 2
    | Debug = 3

type LoggingLocation = 
    | StdOut
    | Trace

type LoggerOptions = 
    { Level : LoggingLevel
      Location : LoggingLocation }
    static member Default = 
        { Level = LoggingLevel.Debug
          Location = LoggingLocation.StdOut }

let mutable private options = LoggerOptions.Default
let private shouldLog (level : LoggingLevel) = level.CompareTo(options.Level) <= 0

let private logString withNewLine (level : LoggingLevel) message = 
    match options.Location with
    | StdOut -> 
        let fn = 
            let isAtLeastError = level.CompareTo(LoggingLevel.Error) <= 0
            match (isAtLeastError, withNewLine) with
            | true, true -> eprintfn
            | true, false -> eprintf
            | false, true -> printfn
            | false, false -> printf
        fn "%O:%s" level message
    | Trace -> 
        match level with
        | LoggingLevel.Error -> System.Diagnostics.Trace.TraceError(message)
        | LoggingLevel.Warning -> System.Diagnostics.Trace.TraceWarning(message)
        | LoggingLevel.Info | LoggingLevel.Debug | _ -> (sprintf "%O:%s" level message) |> System.Diagnostics.Trace.TraceInformation

let configure f = 
    let o' = f options
    options <- o'

let log level (message : string) = 
    if shouldLog level then logString false level message
    else ()

let logn level (message : string) = 
    if shouldLog level then logString true level message
    else ()

let error message = logn LoggingLevel.Error message
let info message = logn LoggingLevel.Info message
let warning message = logn LoggingLevel.Warning message
let debug message = logn LoggingLevel.Debug message
let logfn level format = Printf.kprintf (fun msg -> logString true level msg) format
let errorfn format = logfn LoggingLevel.Error format
let infofn format = logfn LoggingLevel.Info format
let warningfn format = logfn LoggingLevel.Warning format
let debugfn format = logfn LoggingLevel.Debug format
let debugItemfn format item = 
    debugfn format item
    item
