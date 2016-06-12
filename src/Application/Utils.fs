namespace Application

open System

[<AutoOpen>]
module Operators =
    let isNotNull<'a when 'a : null> =
        isNull<'a> >> not

    // Implicit operator
    let inline (!>) (a :^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b) a)

type Result<'a> = 
    | Success of 'a
    | Fail of string list

type AsyncResult<'a> = Async<Result<'a>>

module AsyncResult = 
    let success a = async.Return(Success(a))

[<RequireQualifiedAccess>]
module Result = 
    let private cons head tail = head :: tail
    let retn = Result.Success

    let errorMessages r =
        match r with
        | Success(r) -> []
        | Fail(errors) -> errors
    
    let inline bind f a = 
        match a with
        | Success a' -> f a'
        | Fail errors -> Fail errors

    let combine2 f a b =
        match a, b with
        | Success(a'), Success(b') -> Success(f a' b')
        | Fail(errorsA), Success(_) -> Fail(errorsA)
        | Success(_), Fail(errorsB) -> Fail(errorsB)
        | Fail(errorsA), Fail(errorsB) -> Fail(errorsA @ errorsA)

    let ofOption o =
        match o with
        | Some(a) -> Success(a)
        | None -> Fail([])
    
    let inline map f a = bind (f >> retn) a
    let (>>=) a f = bind f a
    
    let inline apply f a = 
        match f, a with
        | Success(f), Success(a) -> Success(f a)
        | Fail errorsF, Success(_) -> Fail(errorsF)
        | Success(_), Fail errorsA -> Fail(errorsA)
        | Fail errorsF, Fail errorsA -> Fail(errorsF @ errorsA)
    
    let (<*>) = apply
    
    let rec traverseA f list : Result<'b list> = 
        let folder (head : 'a) (tail : Result<'b list>) : Result<'b list> = 
            retn cons <*> (f head) <*> tail
        List.foldBack folder list (retn [])
    
    let traverseM f list : Result<'b list> = 
        let folder head tail = f head >>= (fun h -> tail >>= (fun t -> retn (cons h t)))
        List.foldBack folder list (retn [])
    
    let traverseTuple4<'a, 'b, 'c, 'd, 'r> (f : 'a -> 'b -> 'c -> 'd -> Result<'r>) tuple =
        match tuple with
        | (Success(a), Success(b), Success(c), Success(d)) ->
            f a b c d
        | (ra, rb, rc, rd) ->
            let errMessages = 
                [
                    errorMessages ra;
                    errorMessages rb;
                    errorMessages rc;
                    errorMessages rd;
                ]
                |> List.fold (fun s e -> e @ s) []
            Fail(errMessages)
            

    let fromTry<'a> failMessage (success : bool, value : 'a) = 
        if success then Success(value)
        else Fail([ failMessage ])
    
    let toAsync (a : Result<Async<'a>>) : Async<Result<'a>> = 
        match a with
        | Success asyncA -> async { let! res = asyncA
                                    return Success(res) }
        | Fail errors -> async.Return(Fail errors)
    
    let asyncApply f (a : AsyncResult<'a>) : AsyncResult<'b> = async { let! a' = a
                                                                       return apply f a' }
    let asyncBind f (a : AsyncResult<'a>) : AsyncResult<'b> = async { let! a' = a
                                                                      return bind f a' }
    let asyncMap f (a : AsyncResult<'a>) : AsyncResult<'b> = async { let! a' = a
                                                                     return map f a' }
    
    let bindToAsync (f : 'a -> AsyncResult<'b>) (a : Result<'a>) : AsyncResult<'b> = 
        async { 
            match a with
            | Success a' -> return! f a'
            | Fail errors -> return Fail errors
        }
    
    let bindAsync (f : 'a -> AsyncResult<'b>) (a : AsyncResult<'a>) : AsyncResult<'b> = 
        async { 
            let! (res : Result<'a>) = a
            match res with
            | Success a' -> return! f a'
            | Fail errors -> return Fail errors
        }

module Option = 
    let valueOrFn fn o =
        match o with
        | Some(a) -> a
        | None -> fn()

    let valueOrFn1 fn state o =
        match o with
        | Some(a) -> a
        | None -> fn state

    let fromTry<'a> (success : bool, value : 'a) = 
        if success then Some(value)
        else None

    let cast<'a> (a : obj) =
        match a with
        | :? 'a as a' -> Some(a')
        | _ -> None

        
    let combine2 f (a, b) =
        match (a, b) with
        | (Some(a), Some(b)) -> Some(f a b)
        | _ -> None

    let of2<'a, 'b> =
        combine2 (fun (a :'a) (b : 'b) -> (a, b))

    let combine3 f (a, b, c) =
        match (a, b, c) with
        | (Some(a), Some(b), Some(c)) -> Some(f a b c)
        | _ -> None

    let asNullable<'a when 'a : struct and 'a :> ValueType and 'a : (new : unit -> 'a)> o =
        match o with
        | Some(a) -> Nullable(a)
        | None -> Nullable(Unchecked.defaultof<'a>)

module Seq =
    let ofType<'a> seq =
        seq
        |> Seq.cast<obj>
        |> Seq.choose (function 
            | :? 'a as a -> Some(a) 
            | _ -> None)

module List = 
    let filterOption (list : option<'a> list) = list |> List.choose id

type LoadableItem<'TItemToLoad, 'TLoadedDetail> = 
    | NotLoaded
    | LoadingItem of 'TItemToLoad
    | LoadedItem of 'TItemToLoad * detail : 'TLoadedDetail
    | LoadItemError of 'TItemToLoad * errors : string list

type LoadableBatch<'TItemToLoad, 'TLoadedDetail> = 
    | BatchStarted of 'TItemToLoad
    | BatchUpdated of 'TItemToLoad * detail : 'TLoadedDetail
    | BatchError of 'TItemToLoad * errors : string list
    | BatchFinished of 'TItemToLoad

module Loadable = 
    let unwrapToObject (d : LoadableItem<'TItemToLoad, 'TLoadedDetail>) : obj = 
        match d with
        | NotLoaded -> () :> obj
        | LoadingItem(l) -> l :> obj
        | LoadedItem(_, s) -> s :> obj
        | LoadItemError(_) -> () :> obj
    
    let retn key item = LoadedItem(key, item)
    
    let bind f l = 
        match l with
        | NotLoaded -> NotLoaded
        | LoadingItem(key) -> LoadingItem(key)
        | LoadedItem(key, i) -> f key i
        | LoadItemError(key, err) -> LoadItemError(key, err)
    
    let map fKey fItem l = 
        match l with
        | NotLoaded -> NotLoaded
        | LoadingItem(key) -> LoadingItem(fKey key)
        | LoadedItem(key, i) -> LoadedItem(fKey key, fItem i)
        | LoadItemError(key, err) -> LoadItemError(fKey key, err)
    
    let mapItem f = map id f
    let mapKey f = map f id

module LoadUpdate = 
    let unwrapToObject (d : LoadableBatch<'TItemToLoad, 'TLoadedDetail>) : obj = 
        match d with
        | BatchUpdated(_, l) -> l :> obj
        | BatchError(_) -> () :> obj
        | BatchFinished(_) -> () :> obj
    
    let retn key item = LoadedItem(key, item)
    
    let bind f l = 
        match l with
        | BatchStarted(key) -> BatchStarted(key)
        | BatchUpdated(key, i) -> f key i
        | BatchError(key, err) -> BatchError(key, err)
        | BatchFinished(key) -> BatchFinished(key)
    
    let map fKey fItem l = 
        match l with
        | BatchStarted(key) -> BatchStarted(fKey key)
        | BatchUpdated(key, item) -> BatchUpdated(fKey key, fItem item)
        | BatchError(key, err) -> BatchError(fKey key, err)
        | BatchFinished(key) -> BatchFinished(fKey key)
    
    let mapItem f = map id f
    let mapKey f = map f id

module Action =
    let once f =
        let hasExecuted = ref false
        fun () ->
            if not !hasExecuted then
                f()
                hasExecuted := true

module Func = 
    let idf a =
        fun _ -> a

module Disposable = 
    open System.Reactive.Disposables
    let private swallow f = 
        try 
            f()
        with _ -> ()
    
    let Combine (a : IDisposable) (b : IDisposable) = 
        Disposable.Create(fun () -> 
            b.Dispose |> swallow
            a.Dispose |> swallow)

    let CombineMany (disposables : IDisposable list) = 
        Disposable.Create(fun () -> 
            disposables
            |> List.iter (fun d -> d.Dispose |> swallow))
