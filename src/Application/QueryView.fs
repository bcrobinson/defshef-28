namespace Application

open FsXaml
open System
open WpfUtils

type QueryView = XAML< "QueryView.xaml" >

module QueryViewEx = 
    open QueryItemViewEx
    open System.Windows
    open System.Windows.Forms
    open System.Windows.Controls
    open System.Windows.Input
    

    type QueryView with
        member private this.SetupFromSubscription(update : StoreUpdate<LogQuery>) = 
            let timeOrDefault (d : RoutedPropertyChangedEventArgs<obj>) = 
                match d.NewValue with
                | :? DateTime as d' -> d'
                | _ -> DateTime.UtcNow.AddHours(-1.0)
            
            let dateOrDefault (d : SelectionChangedEventArgs) = 
                if isNull d.AddedItems || d.AddedItems.Count = 0 then DateTime.UtcNow.AddHours(-1.0)
                else 
                    match d.AddedItems.[0] with
                    | :? DateTime as d' -> d'
                    | _ -> DateTime.UtcNow.AddHours(-1.0)
            
            let timeChanged = this.FromTime.ValueChanged |> Observable.map timeOrDefault
            let dateChanged = this.FromDate.SelectedDateChanged |> Observable.map dateOrDefault

            let updateFrom t q =
                { q with From = !> t |> Some }
            
            let subscription =
                (dateChanged, timeChanged)
                |> Observable.combineLatest2 (fun (d, t) -> d.Date.Add(t.TimeOfDay))
                |> Observable.throttle (TimeSpan.FromMilliseconds(float 100))
                |> Observable.subscribe (updateFrom >> update.With)
            subscription

        member private this.SetupPageSizeSubscription(update : StoreUpdate<LogQuery>) =
            let valueOrDefault defaultValue (f : Nullable<float>) = 
                if f.HasValue then uint32 f.Value
                else defaultValue
            
            let subscription = 
                this.MaxRowsToLoad.ValueChanged
                |> Observable.map (fun e -> valueOrDefault 1000u e.NewValue)
                |> Observable.subscribe (fun s -> update.With(fun q -> { q with MaxRowsToLoad = s }))
            
            subscription
        
        member private this.SetupOpenFileButton(update : StoreUpdate<FileName option>) =
            let updateFileName _ =
                use dialog = new OpenFileDialog()
                dialog.RestoreDirectory <- true
                dialog.CheckFileExists <- true
                dialog.Multiselect <- true

                if dialog.ShowDialog() = DialogResult.OK then
                    update.WithValue(FileName(dialog.FileName) |> Some)

            this.OpenFileButton
            |> Button.subscribeClick updateFileName
        
        member private this.SetupAddFilterItemSubscription(update : StoreUpdate<LogQuery>) =
            let addFilterItem _ =
                let f =
                    { Type = Include
                      Field = FilterField.Url
                      Value = String.Empty }
                update.With(LogQuery.AddFilter f)

            let addItemSubscription = 
                this.AddFilterItemButton
                |> Button.getObservable
                |> Observable.subscribe addFilterItem
            
            addItemSubscription
        
        member private this.ObserveUpdates(update : StoreUpdate<LogQuery>, store : Store<LogQuery>) =
            let newFilterItem key _ =
                let removeFilterCommand = this.Resources.["RemoveFilterCommand"] :?> ICommand
                let newItem = QueryItemView()
                newItem.RemoveButton.Command <- removeFilterCommand
                newItem.RemoveButton.CommandParameter <- key
                QueryItemViewEx.setupView key update newItem |> ignore
                newItem :> FrameworkElement

            let filterItemsUpdater = ItemsControl.createMapUpdater newFilterItem this.FilterItems

            let removeFilterCommand = Command.create(fun key ->
                update.With(fun q ->
                    { q with Filters = q.Filters.Remove key}))

            this.Resources.["RemoveFilterCommand"] <- removeFilterCommand

            let toLocalDate =
                Option.map(fun (d : DateTimeOffset)  -> d.ToLocalTime().DateTime)
                >> Option.asNullable
                

            let updateView (s : LogQuery) =
                this.DataContext <- s
                this.FromDate.SelectedDate <- s.From |> toLocalDate
                this.FromTime.Value <- s.From |> toLocalDate
                filterItemsUpdater s.Filters

            store
            |> Store.subscribe updateView


        member this.Setup(queryUpdate : StoreUpdate<LogQuery>, fileNameUpdate : StoreUpdate<FileName option>, queryStore : Store<LogQuery>) =
            [
                this.SetupPageSizeSubscription(queryUpdate)
                this.SetupFromSubscription(queryUpdate)
                this.SetupOpenFileButton(fileNameUpdate)
                this.SetupAddFilterItemSubscription(queryUpdate)
                this.ObserveUpdates(queryUpdate, queryStore)
            ]
            |> Disposable.CombineMany
