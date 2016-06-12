module Application.App

open FsXaml
open System

Logger.configure (fun o -> 
    { o with Level = Logger.LoggingLevel.Debug
             Location = Logger.LoggingLocation.Trace })


// Startup
type App = XAML< "App.xaml" >


[<STAThread>]
[<EntryPoint>]
let main _ = 
    let app = App()
    app.Run(Main.window)
