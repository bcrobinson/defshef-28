namespace Example

open FsXaml
open System

type TextInputView = XAML< "TextInputView.xaml" >

module TextInputViewEx =
    let setupObservable (view : TextInputView) (outputUpdate : StoreUpdate<Output>) =

        let textObs = 
            view.TextInput.TextChanged
            |> Observable.map(fun _ -> view.TextInput.Text)
        
        let numberObs =
            view.NumberInput.TextChanged
            |> Observable.map(fun _ -> view.NumberInput.Text)
            |> Observable.choose(
                fun t -> 
                    match Int32.TryParse t with
                    | true, i -> Some(i)
                    | _ -> None)

        (textObs, numberObs)
        |> Observable.combineLatest2
            (fun (t, i) -> 
                { Text = t
                  Number = i })
        |> Store
        |> Store.connectToUpdate outputUpdate

