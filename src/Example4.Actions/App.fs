module Example.App

open FsXaml
open System

// Startup
type App = XAML< "App.xaml" >


[<STAThread>]
[<EntryPoint>]
let main _ = 
    let app = App()
    app.Run(Main.window)
