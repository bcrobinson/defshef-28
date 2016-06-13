namespace Application

open System

type FileName = | FileName of string

type Request = 
    { Method : string
      Url : string 
      Protocol : string}

type LogRow = 
    { Host : string
      Timestamp : DateTimeOffset
      Request : Request
      ResponseCode  : int
      ResponseSize : int }

type FilterType = 
    | Include
    | Exclude

type FilterField = 
    | Host
    | Url
    static member All = [ Host; Url; ]
    static member DisplayText = 
        function 
        | Host -> "Host"
        | Url -> "Url"

type FilterItem = 
    { Type : FilterType
      Field : FilterField
      Value : string }

type LogQuery =
    { From : DateTimeOffset option
      To : DateTimeOffset option
      MaxRowsToLoad : uint32
      Filters : Map<int, FilterItem> }
    static member EqualWithNoneEmptyFilters lhs rhs = 
        let notEmpty (v : obj) = 
            match v with
            | :? string as s -> not (String.IsNullOrWhiteSpace(s))
            | _ -> false
    
        let withNonEmpty filter = 
            { filter with Filters = filter.Filters |> Map.filter (fun k v -> v.Value |> notEmpty) }
        let lhs' = lhs |> withNonEmpty
        let rhs' = rhs |> withNonEmpty
        lhs' = rhs'

    static member AddFilter filter query =
        let idx = 
            match query.Filters.Count with
            | 0 -> 0
            | _ -> 
                (query.Filters
                |> Seq.map(fun f -> f.Key)
                |> Seq.max) + 1

        { query with Filters = query.Filters.Add(idx, filter) }

type LoadStatus =
    | NotStarted
    | LoadStarted
    | Loading
    | Loaded
    | Error of message : string

type LogsPage = 
    { LastUpdated : DateTimeOffset
      CurrentQuery : LogQuery
      NewLogs : LogRow list
      SkippedLogs : UInt32
      Status: LoadStatus }

type FetchLogs = LogQuery -> IObservable<LogsPage>