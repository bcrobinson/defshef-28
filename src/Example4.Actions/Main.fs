module Example.Main

let window = MainWindow()

// Views
let outputView = window.OutputView :?> OutputView
let textInputView = window.TextInputView :?> TextInputView

// Stores
let (outputUpdate, outputStore) = Store.createStore ({ Text = "some text"; Number = 1 })

// Actions
let getDataAction = Action(Model.getData >> Store)

// Wire up output store to action

let modelDataStore =
    outputStore
    |> Store.connectToAction getDataAction


// Setup views
let textViewDisposable =
    TextInputViewEx.setupObservable textInputView outputUpdate

let outputViewDisposable =
    OutputViewEx.setupView outputView modelDataStore

