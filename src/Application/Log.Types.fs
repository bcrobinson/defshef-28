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
    | ResponseCode
    static member All = [ Host; Url; ResponseCode ]
    
    static member DisplayText = 
        function 
        | Host -> "Host"
        | Url -> "Url"
        | ResponseCode -> "Response Code"

type FilterItem = 
    { Type : FilterType
      Field : FilterField
      Value : obj }

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
            query.Filters
            |> Seq.map(fun f -> f.Key)
            |> Seq.max

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
      Status: LoadStatus }

type FetchLogs = LogQuery -> IObservable<LogsPage>