namespace Example

open FsXaml

type OutputView = XAML< "OutputView.xaml" >

module OutputViewEx =
    let private getUpdater (view : OutputView) =
        fun update ->
            view.CountText.Text <- sprintf "%s - %i" update.Text update.Number

    let setupView (view : OutputView) (store : Store<Output>) =
        let updater = getUpdater view
        store |> Store.subscribe updater
