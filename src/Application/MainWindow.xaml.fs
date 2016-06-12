namespace Application

open Application
open FsXaml
open System

type MainWindow = XAML< "MainWindow.xaml" >

module MainWindowEx = 
    let private getNotificationText (page : LogsPage) =
        let d = DateTime.Now.ToString("s")
        match page.Status with
        | NotStarted ->
            sprintf "Not started loading at %s" d
        | LoadStarted ->
            sprintf "Start loading at %s" d
        | Loading ->
            sprintf "Loaded batch %i logs at %s" (page.NewLogs.Length) d
        | Loaded ->
            sprintf "Finished loading at %s" d
        | LoadStatus.Error(error) ->
            sprintf "Errors updating at %s:%s" d error
    
    type MainWindow with
        member this.QueryView' = this.QueryView :?> Application.QueryView
 
        member this.LogListView' = this.LogListView :?> Application.LogListView

        member this.Setup(pageStore : Store<LogsPage>) =
            pageStore
            |>Store.subscribe(fun s -> 
                this.LastUpdatedText.Text <- getNotificationText s)
