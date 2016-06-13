namespace Application

open FsXaml

type LogListView = XAML< "LogListView.xaml" >

[<CLIMutable>]
type LogListViewModel = 
    { Logs : LogRow list
      CurrentQuery : LogQuery option
      Loading : bool
      ShowEmptyLoading: bool
      LoadErrors : string [] }

module LogListViewEx = 
    let defaultViewModel = 
        { CurrentQuery = None
          Logs = []
          Loading = false
          ShowEmptyLoading = false
          LoadErrors = [||] }
    
    let private stateMapper currentVm (logsUpdate : LogsPage) =
        let addNewRows state rows =
            { state with
                Logs = state.Logs @ rows
                Loading = true
                ShowEmptyLoading = false
                LoadErrors  = [||] }

        let resetRows state rows newQuery =
            { state with
                CurrentQuery = Some(newQuery)
                Logs = rows
                Loading = true
                ShowEmptyLoading = rows.Length = 0
                LoadErrors  = [||] }

        let finishedLoading state =
            { state with 
                Loading = false 
                ShowEmptyLoading = false }

        match logsUpdate.Status with
        | NotStarted ->
            { currentVm with
                ShowEmptyLoading = false
                Loading = false }
        | LoadStarted ->
            resetRows currentVm [] logsUpdate.CurrentQuery
        | Loading when currentVm.CurrentQuery = Some(logsUpdate.CurrentQuery) ->
            addNewRows currentVm logsUpdate.NewLogs
        | Loading ->
            resetRows currentVm logsUpdate.NewLogs logsUpdate.CurrentQuery
        | Loaded ->
            finishedLoading currentVm
        | Error(error) ->
            currentVm

    type LogListView with
        
        member private this.GetSelectedLogObservable() =
            let single (list : System.Collections.IList) =
                if list = null || list.Count <= 0 then None
                else 
                    match list.[0] with
                    | :? LogRow as a -> Some(a)
                    | _ -> None
            
            let selectedItem =
                this.RowGrid.SelectionChanged 
                |> Observable.map (fun c -> single c.AddedItems)
            selectedItem
        
        member private this.CreateUpdater() =
            fun (logUpdate : LogsPage) -> 
                let current = 
                    match this.DataContext with
                    | :? LogListViewModel as s -> s
                    | _ -> defaultViewModel
                let newState = stateMapper current logUpdate
                this.RowCountTextBlock.Text <- sprintf "%i logs" newState.Logs.Length
                this.DataContext <- newState

        member this.Setup(update: StoreUpdate<LogRow option>, logPageStore : Store<LogsPage>) =
            let selectedLog = this.GetSelectedLogObservable()
            let updater = this.CreateUpdater()
            
            let aa = Store.subscribe updater

            let d1 = 
                logPageStore
                |> Store.subscribe updater
            let d2 = 
                selectedLog
                |> Observable.subscribe (fun l -> update.WithValue(l))
            Disposable.Combine d1 d2

