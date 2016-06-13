namespace Example

open System
open System.Reactive.Subjects
      
type Update<'a> =
    | Update of ('a -> 'a)
    member this.Apply(a) =
        match this with
        | Update(f) -> f a

type StoreUpdate<'a> =
    | StoreUpdate of (Update<'a> -> unit)
    
    member this.With f =
        let (StoreUpdate(u)) = this
        u(Update(f))

    member this.WithValue a =
        this.With(fun _ -> a)
    
type Store<'a> =
    | Store of IObservable<'a>

type Action<'a, 'b> =
    | Action of ('a -> Store<'b>)


module Store =
    let createStore<'a when 'a : equality> initialState =
        let subject = new Subject<Update<'a>>()

        let observable : IObservable<'a> =
            subject
            |> Observable.scan (fun state update -> update.Apply state) initialState 
            |> Observable.distinctUntilChanged

        let update u = subject.OnNext(u)

        (StoreUpdate(update), Store(observable))
    
    let map f (Store(s)) =
        let obs = s |> Observable.map f
        Store(obs)

    let combine2 fn (storeA, storeB) =
        let (Store(obsA)) = storeA
        let (Store(obsB)) = storeB

        (obsA, obsB) 
        |> Observable.combineLatest2 (fun (a, b) -> fn a b)
        |> Store

    let connectToUpdate (StoreUpdate(update)) (Store(s)) =
        s |> Observable.subscribe(fun i -> update (Update(fun _ -> i)))

    let connectToAction (Action(f)) (Store(s)) =
        let obs = s |> Observable.switch (fun a -> 
            let (Store(o)) = f a
            o)
        Store(obs)

    let subscribe f (Store(s)) =
        s 
        |> Observable.observeOnDispatcher
        |> Observable.subscribe f



