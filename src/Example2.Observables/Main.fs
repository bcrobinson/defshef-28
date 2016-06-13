module Example.Main

let window = MainWindow()

// Views
let countView = window.CountView :?> DisplayCountView
let incrementView = window.IncrementView :?> IncrementView

// Setup update
let countUpdater = DisplayCountViewEx.getUpdater countView
let incrementObservable = IncrementViewEx.getObservable incrementView

let unsubscribe =
    incrementObservable
    |> Observable.subscribe countUpdater
