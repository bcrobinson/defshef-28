module Application.Main

open Application
open Application.MainWindowEx
open Application.QueryViewEx
open Application.LogListViewEx
open System

module InitialState =
    let fileName = Option<FileName>.None

    let subscriptionLogQuery = 
        { From = DateTimeOffset.Parse("01/07/1995 00:20:00 -04:00") |> Some
          To = None
          MaxRowsToLoad = 200u
          Filters = Map.empty }

    let logsPage = 
        { LastUpdated = DateTimeOffset.MinValue
          CurrentQuery = subscriptionLogQuery
          NewLogs = []
          SkippedLogs = 0u
          Status = LoadStatus.NotStarted }

// Actions
let loadQueryResultsAction = Action(fun (name, query) -> LogReader.loadLogs name query)

// Stores
let (selectedFileUpdate, selectedFileStore) = Store.createStore InitialState.fileName
let (queryUpdate, queryStore) = Store.createStore InitialState.subscriptionLogQuery
let (selectedLogUpdate, selectedLogStore) = Store.createStore<LogRow option> None
let (logsPageUpdate, logsPageStore) = Store.createStore InitialState.logsPage

let logsDisposable =
    let (Store(fileObs)) = selectedFileStore
    let (Store(queryObs)) = queryStore

    (fileObs, queryObs)
    |> Observable.combineLatest2 id
    |> Observable.choose 
        (fun (fn, query) ->
            match fn with
            | Some(fn) -> (fn, query) |> Some
            | None -> None )
    |> Observable.throttle (TimeSpan.FromMilliseconds(float 400))
    |> Store
    |> Store.connectToAction(loadQueryResultsAction)
    |> Store.connectToUpdate(logsPageUpdate)

// Views
let window = MainWindow()

let queryView = window.QueryView'
let logListView = window.LogListView'

let mainWindowDisposable = window.Setup(logsPageStore)
let queryViewDisposable = queryView.Setup(queryUpdate, selectedFileUpdate, queryStore)
let subscriptionLogViewDisposable = logListView.Setup(selectedLogUpdate, logsPageStore)

// Force stores in send updates
queryUpdate.WithForced(id)
selectedLogUpdate.WithForced(id)
logsPageUpdate.WithForced(id)
