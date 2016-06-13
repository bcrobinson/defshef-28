namespace Example

open System
open System.Reactive.Concurrency
open System.Reactive.Linq

module Observable = 
    open System.Collections.Generic

    let observeOnDispatcher (observable : IObservable<'T>) = observable.ObserveOnDispatcher()

    let mapAsync (f : 'a -> Async<'b>) (observable : IObservable<'a>) = 
        Observable.SelectMany(observable, (fun a -> (f a) |> Async.StartAsTask))

    let distinctUntilChanged<'a when 'a : equality> obs =
        let comp = 
            { new IEqualityComparer<'a> with
                member x.Equals(a, b) =
                    let eq = a = b
                    eq
                member x.GetHashCode(a) =
                    a.GetHashCode() }
        Observable.DistinctUntilChanged(obs, comp)

    let switch (f : 'a -> IObservable<'b>) (observable : IObservable<'a>) : IObservable<'b> = 
        let manyObs =
            observable
            |> Observable.map f
        Observable.Switch(manyObs)
    
    let combineLatest2 (resultSelector : 'a * 'b -> 'c) observables : IObservable<'c> = 
        let (oa, ob) = observables
        Observable.CombineLatest(oa, ob, fun a b -> resultSelector (a, b))
    
    let combineLatest3 (resultSelector : 'a * 'b * 'c -> 'd) observables : IObservable<'d> = 
        let (oa, ob, oc) = observables
        Observable.CombineLatest(oa, ob, oc, fun a b c -> resultSelector (a, b, c))
    
    let combineLatest4 (resultSelector : 'a * 'b * 'c * 'd -> 'e) observables : IObservable<'e> = 
        let (oa, ob, oc, od) = observables
        Observable.CombineLatest(oa, ob, oc, od, fun a b c d -> resultSelector (a, b, c, d))
    
    let whenDifferentBy (f : 'a -> 'a -> bool) obs = 
        obs
        |> Observable.scan 
            (fun (prev, _) current -> 
               let next = Some(current)
               match prev with
               | None -> (next, next)
               | Some(p) when not (f p current) -> (next, next)
               | _ -> (next, None))
            (None, None)
        |> Observable.choose snd
    
    let whenDifferent obs = 
        obs |> whenDifferentBy (=)

    let throttle (timespan : TimeSpan) observable = Observable.Throttle(observable, timespan)
    
    type ObservableBuilder() = 
        member __.Bind(m : IObservable<_>, f : _ -> IObservable<_>) = m.SelectMany(f)
        member __.Combine(comp1, comp2) = Observable.Concat(comp1, comp2)
        member __.Delay(f : _ -> IObservable<_>) = Observable.Defer(fun _ -> f())
        member __.Zero() = Observable.Empty(Scheduler.CurrentThread :> IScheduler)
        member __.For(sequence, body) = Observable.For(sequence, Func<_, _> body)
        member __.TryWith(m : IObservable<_>, h : #exn -> IObservable<_>) = Observable.Catch(m, h)
        member __.TryFinally(m, compensation) = Observable.Finally(m, Action compensation)
        member __.Using(res : #IDisposable, body) = 
            Observable.Using((fun () -> res), Func<_, _> body)
        member __.While(guard, m : IObservable<_>) = Observable.While(Func<_> guard, m)
        member __.Yield(x) = Observable.Return(x, Scheduler.CurrentThread)
        member __.YieldFrom m : IObservable<_> = m
    
    let observe = ObservableBuilder()
