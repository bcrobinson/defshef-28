namespace Example

open FsXaml
open System

type TextInputView = XAML< "TextInputView.xaml" >

module TextInputViewEx =
    let setupObservable (view : TextInputView) (yourTextUpdate : StoreUpdate<string>) =
        view.TextInput.TextChanged
        |> Observable.map(fun _ -> view.TextInput.Text)
        |> Store
        |> Store.connectToUpdate yourTextUpdate
