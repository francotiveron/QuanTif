namespace QuanTif.Trading

open Deedle
open System
open QuanTif.Common
open Microsoft.FSharp.Reflection
open System.Collections.Generic

//type TradingPosition = | Flat | Long | Short

//type PriceSeries = 
//    | Instrument of seq<DateTime*float*float*float*float*int>
//    | Indicator of seq<DateTime*float>
//type History = seq<string * PriceSeries>

type IBacktestStatus = 
    abstract member GetInstrument : symbol:string * offset:int -> OHLCV option
    abstract member GetOpen : symbol:string * offset:int -> float option
    abstract member GetClose : symbol:string * offset:int -> float option
    abstract member Trade : symbol:string * shares:int -> bool
    abstract member GetIndicator : id:string * offset:int -> float option
    abstract member GetTime : offset:int -> DateTime
    abstract member GetPosition : symbol:string -> (int * float) option

type BackTest = {
    Market : seq<MarketData>
    Strategy : IBacktestStatus -> unit
}

type TradingOperation = {
    Time : DateTime
    Symbol : string
    Shares : int
    Price : float
}

type BacktestReport = {
    Operations : TradingOperation list
    Balance : float
}

type BacktestStatus internal (bt:BackTest) = 
    let toFrame marketData = 
        match marketData with
        | Instrument data -> [data.Symbol, data.MarketData |> series] |> frame
        | Indicator data -> [data.Id, data.MarketData |> series] |> frame
    let frame = bt.Market |> Seq.map toFrame |> Frame.mergeAll |> Frame.sortRowsByKey
    let mutable time = 1
    let getTime offset = frame.GetRowKeyAt(int64 (time + offset))
    let n = frame.RowCount - 1
    let mutable operations : TradingOperation list = []
    let portfolio = Dictionary<string, int * float>()
    let mutable balance = 0.
    let toOption (ov:OptionalValue<_>) = 
        match ov with
        | OptionalValue.Present v -> Some v
        | OptionalValue.Missing -> None
    member internal this.Next() = if time < n then time <- time + 1; true else false
    member internal this.FinalReport = 
        { 
        Operations = List.rev operations
        Balance = balance + (portfolio.Values |> Seq.fold (fun y (shares, charge) -> y + (float shares) * charge) 0.)
        }
    interface IBacktestStatus with
        member this.GetInstrument(symbol, offset) = 
            let col = frame.GetColumn<OHLCV>(symbol)
            col.TryGetAt(time + offset) |> toOption
        member this.GetOpen(symbol, offset) = (this :> IBacktestStatus).GetInstrument(symbol, offset) |> Option.map (fun ohlcv -> ohlcv.O)
        member this.GetClose(symbol, offset) = (this :> IBacktestStatus).GetInstrument(symbol, offset) |> Option.map (fun ohlcv -> ohlcv.C)
        member this.Trade(symbol, shares) = 
            match (this :> IBacktestStatus).GetOpen(symbol, 0) with
            | Some todayOpen -> 
                match portfolio.TryGetValue(symbol) with
                | true, (curShares, chargePrice) ->
                    if sign curShares = sign shares then
                        operations <- {Time = getTime 0; Symbol = symbol; Shares = shares; Price = todayOpen} :: operations
                        let newShares = curShares + shares
                        let newCharge = ((float curShares) * chargePrice + (float shares) * todayOpen) / (float newShares)
                        portfolio.[symbol] <- (newShares, newCharge)
                    else
                        let newShares = curShares + shares
                        if abs shares > abs curShares then 
                            operations <- {Time = getTime 0; Symbol = symbol; Shares = -curShares; Price = todayOpen} :: operations
                            balance <- balance + (float curShares) * todayOpen
                            operations <- {Time = getTime 0; Symbol = symbol; Shares = newShares; Price = todayOpen} :: operations
                            balance <- balance - (float newShares) * todayOpen
                            portfolio.[symbol] <- (newShares, todayOpen)
                        else 
                            operations <- {Time = getTime 0; Symbol = symbol; Shares = -shares; Price = chargePrice} :: operations
                            balance <- balance + (float shares) * chargePrice
                            if newShares <> 0 then portfolio.[symbol] <- (newShares, chargePrice)
                            else portfolio.Remove(symbol) |> ignore
                 | _ -> 
                    operations <- {Time = getTime 0; Symbol = symbol; Shares = shares; Price = todayOpen} :: operations
                    balance <- balance - (float shares) * todayOpen
                    portfolio.[symbol] <- (shares, todayOpen)
                true
            | _ -> false
        member this.GetIndicator(id, offset) = 
            let col = frame.GetColumn<float>(id)
            col.TryGetAt(time + offset) |> toOption
        member this.GetTime(offset) = getTime offset
        member this.GetPosition(symbol) = 
            match portfolio.TryGetValue(symbol) with
            | true, ret -> Some ret
            | _ -> None

module Backtest =
    let backtest test = 
        let rec iterate (status:BacktestStatus) = 
            status :> IBacktestStatus |> test.Strategy
            if status.Next() then iterate status else status

        (BacktestStatus(test) |> iterate).FinalReport
