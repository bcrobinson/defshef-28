namespace Example

open FsXaml
open System

type IncrementView = XAML< "IncrementView.xaml" >

module IncrementViewEx =
    let setupObservable (view : IncrementView) (numberUpdate : StoreUpdate<int>) =
        view.IncrementButton.Click
        |> Observable.scan (fun state _ -> state + 1) 1
        |> Store
        |> Store.connectToUpdate numberUpdate

