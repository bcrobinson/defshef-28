module Application.WpfUtils

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data

// Helpers
let private tryGetTagFromElement<'a> (el : FrameworkElement) = 
    match el.Tag with
    | :? 'a as i -> Some(i)
    | _ -> None

// Value Converters
type ValueConverter<'a>(mapper : 'a -> obj) = 
    interface IValueConverter with
        member this.Convert(value, targetType, parameter, culture) = 
            match value with
            | :? 'a as a -> mapper a
            | _ -> value
        
        member this.ConvertBack(value, targetType, parameter, culture) = 
            raise (NotImplementedException())

type ValueConverter2<'source, 'target>(mapTo : 'source -> 'target, mapFrom : 'target -> 'source) = 
    interface IValueConverter with
        
        member this.Convert(value, targetType, parameter, culture) = 
            match value with
            | :? 'source as a -> mapTo a :> obj
            | _ -> value
        
        member this.ConvertBack(value, targetType, parameter, culture) = 
            match value with
            | :? 'target as a -> mapFrom a :> obj
            | _ -> value

// Check Box
let getCheckedObservable (checkbox : CheckBox) = 
    let checkedObservable = checkbox.Checked |> Observable.map (fun _ -> true)
    let uncheckedObservable = checkbox.Unchecked |> Observable.map (fun _ -> false)
    Observable.merge checkedObservable uncheckedObservable

type TemplateSelector<'a>(resourceKeySelector : 'a -> string) = 
    inherit DataTemplateSelector()
    override this.SelectTemplate(obj, depObj) : DataTemplate = 
        match obj with
        | :? 'a as a -> 
            match depObj with
            | :? FrameworkElement as fe -> 
                let key = resourceKeySelector a
                match fe.FindResource(key) with
                | :? DataTemplate as t -> t
                | _ -> null
            | _ -> null
        | _ -> null

module Command = 
    open System.Windows.Input
    
    type private Cmd(execute, canExecute) = 
        interface ICommand with
            member this.add_CanExecuteChanged (handler) = 
                CommandManager.RequerySuggested.AddHandler(handler)
            member this.remove_CanExecuteChanged (handler) = 
                CommandManager.RequerySuggested.RemoveHandler(handler)
            member this.CanExecute(obj : obj) = canExecute (obj)
            member this.Execute(obj : obj) = execute (obj)
    
    let private castDown<'a, 'b> (f : 'a -> 'b) defaultB = 
        let castedFn (o : obj) : 'b = 
            let at = typeof<'a>
            let bt = typeof<unit>
            if (at = bt) then 
                let a = () :> obj :?> 'a
                f a
            else 
                match o with
                | :? 'a as a -> f a
                | _ -> defaultB
        castedFn
    
    let createWithCheck<'a> execute canExecute = 
        let ex = castDown<'a, unit> execute ()
        let canEx = castDown<'a, bool> canExecute false
        Cmd(ex, canEx) :> ICommand
    
    let create<'a> execute = createWithCheck<'a> execute (fun _ -> true)

// ComboBox
[<RequireQualifiedAccess>]
module ComboBox = 
    let createUpdater<'item when 'item : equality> (comboBox : ComboBox) = 
        let selectItem item = 
            let foundItem = 
                comboBox.Items
                |> Seq.cast<FrameworkElement>
                |> Seq.tryFind (fun el -> tryGetTagFromElement el = Some(item))
            match foundItem with
            | Some(i) -> comboBox.SelectedItem <- i
            | None -> Logger.errorfn "Error updating ComboBox, item could not be found:%A" item
        selectItem
    
    let getSelectedItem<'item> (comboBox : ComboBox) = 
        match comboBox.SelectedItem with
        | :? FrameworkElement as fe -> tryGetTagFromElement<'item> fe
        | _ -> None
    
    let createObservable<'item when 'item : equality> (items : 'item list) displayTextMapper 
        (comboBox : ComboBox) = 
        let tryGetTagAsItem (args : SelectionChangedEventArgs) = 
            if args.AddedItems.Count > 0 then 
                match args.AddedItems.[0] with
                | :? FrameworkElement as el -> tryGetTagFromElement<'item> el
                | _ -> None
            else None
        
        let comboItems = 
            items |> List.map (fun i -> 
                         let item = ComboBoxItem()
                         item.Tag <- i
                         let displayText : string = displayTextMapper i
                         item.Content <- displayText
                         item)
        
        comboBox.Items.Clear()
        for i in comboItems do
            comboBox.Items.Add(i) |> ignore
        comboBox.SelectionChanged |> Observable.choose tryGetTagAsItem

// ItemsControl
[<RequireQualifiedAccess>]
module ItemsControl = 
    let createMapUpdater (elementFn : 'key -> 'item -> FrameworkElement) 
        (itemsControl : ItemsControl) : Map<'key, 'item> -> unit = 
        let elementHasKey key (el : FrameworkElement) = 
            match el.Tag with
            | :? 'key as k -> 
                if k = key then Some(el)
                else None
            | _ -> None
        
        let findEl key = 
            itemsControl.Items
            |> Seq.ofType<FrameworkElement>
            |> Seq.tryPick (elementHasKey key)
        
        let findOrCreateEl key item = 
            let el = findEl key |> Option.valueOrFn (fun () -> elementFn key item)
            el.DataContext <- item
            el.Tag <- key
            el
        
        let getNewElementList (map : Map<'key, 'item>) = 
            map
            |> Map.toList
            |> List.sortBy fst
            |> List.map (fun (key, item) -> findOrCreateEl key item)
        
        let update map = 
            let items = getNewElementList map
            itemsControl.Items.Clear()
            items |> List.iter (itemsControl.Items.Add >> ignore)
        
        update

[<RequireQualifiedAccess>]
module Button = 
    let getObservable (button : Button) = button.Click |> Observable.map ignore
    let onClick f (button : Button) = button.Click |> Observable.map (fun _ -> f())
    let subscribeClick f (button : Button) = button.Click |> Observable.subscribe (fun _ -> f())

[<RequireQualifiedAccess>]
module ContentControl = 
    let createUpdater (presenter : ContentControl) (selector : 'a -> (string * obj)) : 'a -> unit = 
        let updateTemplate templateKey = 
            let keyDiffers = 
                match presenter.Tag with
                | :? string as s -> templateKey <> s
                | _ -> true
            if keyDiffers then 
                let template = 
                    match presenter.FindResource(templateKey) with
                    | :? DataTemplate as t -> t
                    | _ -> null
                presenter.ContentTemplate <- template
                presenter.Tag <- templateKey
            ()
        
        let updater (state : 'a) = 
            let (templateKey, ctx) = selector state
            updateTemplate templateKey
            presenter.DataContext <- ctx
            presenter.Content <- ctx
            ()
        
        updater

[<RequireQualifiedAccess>]
module TextBox = 
    let getTextChanged (textBox : TextBox) = 
        let getText (args : TextChangedEventArgs) = 
            let text = (args.Source :?> TextBox).Text
            text
        textBox.TextChanged |> Observable.map getText
