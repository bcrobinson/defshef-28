namespace Example

open FsXaml

type OutputView = XAML< "OutputView.xaml" >

module OutputViewEx =
    let private getUpdater (view : OutputView) =
        fun update ->
            view.CountText.Text <- update

    let setupView (view : OutputView) (store : Store<string>) =
        let updater = getUpdater view
        store |> Store.subscribe updater
