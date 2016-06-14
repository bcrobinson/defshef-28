- title : FsReveal
- description : Introduction to FsReveal
- author : Karlkim Suwanmongkol
- theme : night
- transition : default

***

# Defsheff 28

## A Paratcal guide to making UI apps in F#

Ben C Robinson

***

### F# Basics

1. Functional first based on OCaml
1. Compiles to IL so can interact with the entire .Net ecosystem

---

## Syntax - Functions

First class functions

####
    let double x =  x * 2

    let i = 3
    let d = double
    printfn "result: %i * 2 = %i" i (d i)

```
result: 3 * 2 = 6
```

---

## Partial application

All funtions are partially applied by default

There is no difference between:

- a function that takes two values and returns a value
- a function that takes a single value and returns a function

####
    // Bad F# (this takes a single tuple)
    let multiply (x, y) = x * y

    // Good F#
    let multiply x y = x * y

---

## Partial application

####
    let multiply x y = x * y
    val multiply : x:int -> y:int -> int

    let double x = multiply 2
    val double : x:int -> int

    let multiplyBad (x, y) = x * y
    val multiplyBad : x:int * y:int -> int

#### Higher order functions
    let apply f x = f x
    val apply : f:('a -> 'b) -> x:'a -> 'b

---

## Records

####
    type Person =
        { Name : string
          DOB : DateTimeOffset
          FavouriteColour : string }

    // Type inference on records
    let p =
       { Name = "Ben"
         BOB = DateTimeOffset.Parse("1989/01/21")
         FavouriteColour = "it depends" }

    // They're immutable, create a new value with update
    let p2 = { p with FavouriteColour = "Spotted" }

---

## Discriminated Unions

####
    // Simple  "enum style"
    type Team =
        | Red
        | Blue

    // Addative type
    type Result<'a> =
        | Success of a : 'a
        | Fail of string

    let yay = Success(42)
    let nay = Fail("not a number")

---

## Pattern Matching

####
    let multiplyResult r x =
        match r with
        | Success(i) -> Success(i * 2)
        | Fail (err) -> Fail(err)


```
multipleResult yay 2
val it : Result<int> = Success 84
```
```
multipleResult nay 2
val it : Result<int> = Fail "not a number"
```

---

## Pattern matching - Bind

####
    let bind r f =
        match r with
        | Success(a) -> f a
        | Fail(err) -> Fail(err)

    val bind : r:Result<'a> -> f:('a -> Result<'b>) -> Result<'b>

####
    // And example of "piping" results
    """  42  """
    |> bind parseInt
    |> bind (fun i -> Success(i * 3))
    |> bind (
        function
        | Success(i) when i < 100 -> Success(i)
        | Success(i) -> Fail("Number too big")
        | Fail(err) -> Fail(err))

---

## Single Case Discrimited Unions

A very helpful pattern to give type checking to basic types

####
    let PersonId = | PersonId of int

    let getPerson (PersonId(id)) =
        // fetch person

####
    let id = PersonId(42)
    let p = getPerson id

####
    // This will fail to compile
    let id = 42
    let p = getPerson id

***

# UI Patterns

---

## MVVM

Model ViewModel Model

```
               +-------+
               | Model |
               +-----^-+
                |    |
    Data        |    |         Commands
    flows    +--v---------+    used to send data
    down     | View Model |    and actions up
             +-------^----+
                 |   |
                 |   |
               +-v-----+
               | View  |
               +-------+
```

---

MVVM with F#?

---

Yes, but it doesn't look like f#

####
    module Model =
        let fetch count =
            // get some value from model

    type MainViewModel() as self =
        inherit ViewModelBase()

        member self.Count = 0 with get, set
        member self.Data = "" with get, set

        member self.Increment()
            this.Count <- this.Count + 1

        member self.FetchData()
            this.Data <- Model.fetch self.Count

***

## Example 1 - Events

***

So where does FRP come into this?

---

[What is Reactive Programming? - Paul Stovell](http://paulstovell.com/blog/reactive-programming)

Traditional programming

####
    let a = 10
    let b = a + 1
    printfn "(1) b = %i" b
    let a = 11
    printfn "(2) b = %i" b

```
(1) b = 11
(2) b = 11
```

Reactive Programming

####
    let a = 10
    let! b = (observe a) + 1
    printfn "(1) b = %i" b
    let a = 11
    printfn "(2) b = %i" b

```
(1) b = 11
(2) b = 12
```
'b' is not a single value, but a stream of values

***

## Reactive Extensions

An implementation of FRP based around IObservable<'a>

####
    [lang=cs]
    public interface IObservable<out T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }

    public interface IObserver<in T>
    {
        void OnNext(T value);
        void OnError(Exception error);
        void OnCompleted();
    }

---

### Examples

####
    // Subscribing
    view.Button.Click
    |> Observable.subscribe(fun _ -> doSomething())

####
    // Extending streams
    view.ItemChanged
    |> Observable(fun item -> mapItem i)
    |> Observable.subscribe(fun i -> doSomething i)

####
    // Combining
    let itemAUpdate = view.ItemAChanged
    let itemBUpdate = view.ItemAChanged

    (itemAUpdate, itemBUpdate)
    |> Observable.combineLatest2 (fun a b -> combine a b)
    |> Observable.subscribe(fun i -> doSomething i)

---

## Example 2 - Observables

***

## Stores

Putting this all together to create domain specific types

####
    type Store<'a> = | Store of IObservable<'a>

    type Update<'a> = | Update of ('a -> 'a)

    type StoreUpdate<'a> = | StoreUpdate of (Update<'a> -> unit)

####
    module Store =
        let createStore<'a> initialState
            : StoreUpdate<'a> * Store<'a>

        let map f (store : Store<'a>) : Store<'b>

        let combine2 fn (storeA, storeB) : Store<'c>

        let connectToUpdate (update : StoreUpdate<a'>)
            (store : Store<a'>)

        let subscribe f (store :(Store<'a>)
---

## Example 3 - Stores

***

## Actions

So we can update from UI interactions
But how do we interact with business logic (e.g. read files, call http methods)

---

Calls onto the model should always be asynschronous (don't block UI)

We can model this with observables

####
    // all model calls must have signature
    'a -> IObvservable<'b>

So the model will:

1. do work in the background
1. then call OnNext on stream to return value
1. (can also return multiple values)

---

## The action type

####
    type Action<'a, 'b> =
        | Action of ('a -> Store<'b>)

How to use it
####
    let getModelData count =
        observe {
            for i in 0..count do
                yield count
        } |> Store

    let getModelDataAction = Action(getModelData)

    let modelStore =
        numberSore |> Store.connectToAction getModelDataAction

```
val modelStore : Store<int>
```

---

#### But what does the output stream look like?

####
    getData 3

```
3
3
3
```

---

#### What happens when the action is called before the previous stream has finished?

[Combining Sequences - IntoToRx.Com](http://www.introtorx.com/content/v1.0.10621.0/12_CombiningSequences.html)

We use Observable.Switch which will return output from last stream

```
getData --1--2--3---|
Stream1  -1|
Stream2      -2---2|
Stream3         -3--3---3|
Outout  --1---2--3--3---3|
```

---

## Example 4 - Actions