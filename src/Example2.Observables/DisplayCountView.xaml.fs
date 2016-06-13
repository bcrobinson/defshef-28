namespace Example

open FsXaml
open System

type DisplayCountView = XAML< "DisplayCountView.xaml" >

module DisplayCountViewEx =
    let getUpdater (view : DisplayCountView) =
        fun count ->
            view.CountText.Text <- sprintf "Count: %i" count
