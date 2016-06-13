namespace Example

open FsXaml
open System

type IncrementView = XAML< "IncrementView.xaml" >

module IncrementViewEx =
    let getObservable (view : IncrementView) =
        view.IncrementButton.Click
        |> Observable.scan (fun state _ -> state + 1) 1

