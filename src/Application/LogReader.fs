namespace Application

module LogReader = 
    open Application
    open Application.Observable
    open System
    open System.IO
    open System.Globalization
    open System.Reactive.Threading.Tasks
    
    let private openFile (FileName(fileName)) =
        try
            Logger.infofn "open file:%s" fileName
            let stream = File.OpenRead(fileName)
            new StreamReader(stream) |> Success
        with 
            error -> Fail([ error.Message ])
    
    let private parseRequest (line: string) : Result<Request> =
        let parts = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        if parts.Length <> 3 then
            Fail(["Invalid number of parts in request"])
        else
            { Method = parts.[0]
              Url = parts.[1]
              Protocol = parts.[2] }
            |> Success

    let private parseRow (line : string) : Result<LogRow> =
        let sub startIdx endIdx =
            line.Substring(startIdx, endIdx - startIdx).Trim()

        let fromTry msg (b, v) =
            match (b, v) with
            | (true, v) -> Success(v)
            | _ -> Fail([msg])

        let hostEndIdx = line.IndexOf(" - - ")
        let host = sub 0 hostEndIdx
        
        let dateStartIdx = hostEndIdx + 6
        let dateEndIdx = line.IndexOf("]")
        let dateStr = sub dateStartIdx dateEndIdx
        let date = 
            DateTimeOffset.TryParseExact(dateStr, "dd/MMM/yyyy:hh:mm:ss K", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
            |> fromTry "Invalid datetime"

        let requestStartIdx = dateEndIdx + 3
        let requestEndIdx = line.IndexOf("\"", requestStartIdx + 1)
        let request = sub requestStartIdx requestEndIdx |> parseRequest
        
        let statusStartIdx = requestEndIdx + 1
        let statusEndIdx = line.IndexOf(" ", statusStartIdx + 1)
        let statusStr = sub statusStartIdx statusEndIdx
        let status = 
            Int32.TryParse(statusStr)
            |> fromTry "Invalid status code"

        let bytesStartIdx = statusEndIdx + 1
        let bytesStr = sub bytesStartIdx line.Length
        let bytes =
            if String.Equals(bytesStr, "-", StringComparison.OrdinalIgnoreCase) then
                0 |> Success
            else
                Int32.TryParse(bytesStr)
                |> fromTry "Invalid response bytes"

        (request, date, status, bytes)
        |> Result.traverseTuple4
            (fun r d s b ->
                { Host = host
                  Timestamp = d
                  ResponseCode = s
                  ResponseSize = b
                  Request = r }
                |> Success)

    let private readBatchLines count (stream : StreamReader) =
        async {
            let mutable i = 0
            let mutable rows = []
            while rows.Length < count && not stream.EndOfStream do
                i <- i + 1
                let! r = stream.ReadLineAsync() |> Async.AwaitTask
                rows <- r :: rows
            Logger.infofn "read batch, %i rows" rows.Length
            return rows
        }

    let private filterRows query rows : LogRow list * uint32 =
        let satisfiesFrom row =
            match query.From with
            | Some(d) -> row.Timestamp >= d
            | None -> true

        let satisfiesTo row =
            match query.To with
            | Some(d) -> row.Timestamp <= d
            | None -> true

        let satifiesFilter filter row =
            let contains field value = 
                if String.IsNullOrWhiteSpace(value) then
                    true
                else if String.IsNullOrWhiteSpace(field) then
                    false
                else
                    field.ToUpperInvariant().Contains(value.ToUpperInvariant())
            
            let fieldValue = 
                match filter.Field with
                | FilterField.Host ->
                    row.Host
                | FilterField.Url ->
                    row.Request.Url
            
            let isMatch = contains fieldValue filter.Value

            match filter.Type with
            | Include -> isMatch
            | Exclude -> not isMatch

        let satisfiesFilters row =
            query.Filters |> Seq.toList |> List.forall (fun f -> satifiesFilter f.Value row)

        let rowSatisfiesQuery row =
            satisfiesFrom row && 
            satisfiesTo row &&
            satisfiesFilters row
            
        let filteredRows = 
            rows |> List.filter rowSatisfiesQuery
            
        (filteredRows, uint32 (rows.Length - filteredRows.Length))

    let private readBatch count (stream : StreamReader) : Async<Result<LogRow list>> =
        async {
            let! rows = stream |> readBatchLines count
            return rows |> Result.traverseA parseRow
        }

    let loadLogs fileName (query : LogQuery) : Store<LogsPage> = 
        Logger.infofn "Start query load, maxCount:%i" query.MaxRowsToLoad

        let errorPage (errors : string list) =
            let errMsg = String.Join(", ", errors)
            { LastUpdated = DateTimeOffset.UtcNow
              CurrentQuery = query
              NewLogs = []
              SkippedLogs = 0u
              Status = LoadStatus.Error(errMsg) }

        let getPage =
            function
            | Success(rows) ->
                let (filteredRows, skipped) = filterRows query rows
                { LastUpdated = DateTimeOffset.UtcNow
                  CurrentQuery = query
                  NewLogs = filteredRows
                  SkippedLogs = skipped
                  Status = LoadStatus.Loading }
            | Fail(errors) ->
                errorPage errors

                
        let startPage() =
            { LastUpdated = DateTimeOffset.UtcNow
              CurrentQuery = query
              NewLogs = []
              SkippedLogs = 0u
              Status = LoadStatus.LoadStarted }

        let finishPage() =
            { LastUpdated = DateTimeOffset.UtcNow
              CurrentQuery = query
              NewLogs = []
              SkippedLogs = 0u
              Status = LoadStatus.Loaded }

        match (openFile fileName) with
        | Fail(errors) ->
            errorPage errors
            |> Observable.retn
            |> Store
        | Success(reader) ->
            let maxBatchSize = 100

            observe {
                use r = reader
                let mutable count = 0u
                try
                    yield startPage()

                    while not r.EndOfStream && count < query.MaxRowsToLoad do
                        let rowsToRead = Math.Min(maxBatchSize, int32 (query.MaxRowsToLoad - count))
                        
                        let! batch = (r |> readBatch rowsToRead |> Async.StartAsTask).ToObservable()
                        let page = getPage batch
                        
                        count <- count + (uint32 page.NewLogs.Length)
                        Logger.infofn "Loaded %i rows, skipped:%i" count page.SkippedLogs

                        yield page

                    yield finishPage()
                with error ->
                    yield errorPage [error.Message]
            }
            |> Store

