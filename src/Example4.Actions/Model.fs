namespace Example

open Example.Observable
open System
open System.Threading.Tasks
open System.Reactive.Concurrency
open System.Reactive.Linq
open System.Reactive.Threading.Tasks

type Output =
    { Text : string
      Number : int }

module Model =

    let getData output =
        if output.Number <= 0 then
            Observable.retn "Bad number"
        else 
            observe {
                for i in 0..output.Number do
                    let! _ = Task.Delay(1500).ToObservable()
                    yield sprintf "Model data %i - %s" i output.Text
            }