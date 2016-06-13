namespace Application

open FsXaml
open System
open WpfUtils

type QueryItemView = XAML< "QueryItemView.xaml">

module QueryItemViewEx = 
    open System.Reactive.Subjects

    let createView() = QueryItemView()
    
    let getFilter (view : QueryItemView) =
        let field = ComboBox.getSelectedItem<FilterField> view.FilterFieldCombo
        let fieldType = ComboBox.getSelectedItem<FilterType> view.FilterTypeCombo

        let text =
            if String.IsNullOrWhiteSpace(view.FilterText.Text) then
                None
            else
                Some(view.FilterText.Text)

        (fieldType, field, text)
        |> Option.combine3 (fun t f v -> 
            { Type = t
              Field = f
              Value = v })
              
    let createUpdater (view : QueryItemView) : FilterItem -> unit = 
        let typeUpdater = ComboBox.createUpdater view.FilterTypeCombo
        let fieldUpdater = ComboBox.createUpdater view.FilterFieldCombo
        fun filter -> 
            typeUpdater filter.Type
            fieldUpdater filter.Field
            view.FilterText.Tag <- filter.Value
            view.FilterText.Text <- filter.Value.ToString()

    let setupView key (update : StoreUpdate<LogQuery>) (view : QueryItemView) : IDisposable =
        let updater = createUpdater view
        
        view.DataContextChanged.Add(fun args -> 
            match args.NewValue with
            | :? FilterItem as item -> updater item
            | _ -> ())
        
        let itemToText item = 
            match item with
            | Include -> "Include"
            | Exclude -> "Exclude"

        let typeObservable = 
            view.FilterTypeCombo |> ComboBox.createObservable [ Include; Exclude ] itemToText

        let fieldObservable = 
            view.FilterFieldCombo
            |> ComboBox.createObservable FilterField.All FilterField.DisplayText

        let valueObservable = view.FilterText |> TextBox.getTextChanged
        
        let filterSubject = new Subject<FilterItem>()

        let queryObs = 
            (typeObservable, fieldObservable, valueObservable)
            |> Observable.combineLatest3 (fun (t, f, v) -> 
                   { Type = t
                     Field = f
                     Value = v })
            |> Observable.subscribe (fun i -> filterSubject.OnNext(i))

        filterSubject 
        |> Observable.whenDifferent
        |> Observable.subscribe (fun i -> 
            update.With(fun q -> { q with Filters = q.Filters.Add(key, i) }))
