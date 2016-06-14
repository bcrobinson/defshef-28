namespace Example

open FsXaml

type IncrementView = XAML< "IncrementView.xaml" >

module IncrementViewEx =
    let setupObservable (view : IncrementView) (numberUpdate : StoreUpdate<int>) =
        view.IncrementButton.Click
        |> Event.add(fun _ -> numberUpdate.With(fun i -> i + 1))
        
