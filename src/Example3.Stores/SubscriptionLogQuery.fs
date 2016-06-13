module LogView.Application.Store.SubscriptionLogQuery

open LogView

let initialState = 
    { TakeLastMins = 60u
      PageSize = 50u
      Page = 0u }
    
//let private queryUpdateSubject = new Subject<SubscriptionLogQuery -> SubscriptionLogQuery>()
//let queryObservable : IObservable<SubscriptionLogQuery> = queryUpdateSubject |> Observable.scanMap initialState
//let private update f = queryUpdateSubject.OnNext f
//let onPageChange page = update (fun q -> { q with Page = page })
//let onPageSizeChange page = update (fun q -> { q with Page = page })
