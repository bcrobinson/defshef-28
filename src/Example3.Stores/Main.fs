module Example.Main

let window = MainWindow()

// Views
let outputView = window.OutputView :?> OutputView
let incrementView = window.IncrementView :?> IncrementView
let textInputView = window.TextInputView :?> TextInputView

// Stores
let (textUpdate, textStore) = Store.createStore "Initial text"
let (numberUpdate, numberStore) = Store.createStore 1

// Combine text & number stores into output store
let outputStore =
    (textStore, numberStore)
    |> Store.combine2
        (fun text number ->
            { Text = text
              Number = number })


// Setup views
let incrementViewDisposable =
    IncrementViewEx.setupObservable incrementView numberUpdate

let textViewDisposable =
    TextInputViewEx.setupObservable textInputView textUpdate

let outputViewDisposable =
    OutputViewEx.setupView outputView outputStore

// Force initial updates
textUpdate.With(id)
numberUpdate.With(id)

