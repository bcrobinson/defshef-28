module Example1.Main

let window = MainWindow()

let setCount count =
    window.CountText.Text <- sprintf "Count: %i" count

let unsubscribe =
    window.IncrementButton.Click
    |> Event.scan(fun state _ -> state + 1) 1
    |> Event.add setCount