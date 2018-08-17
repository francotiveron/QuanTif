namespace QuanTif.Feed

open System
open FSharp.Data
open QuanTif.Common

module AlphaVantage = 
    type Timeframe = | M1 | M5 | M15 | M30 | M60 | DAY | WEEK | MONTH with 
        member this.ToQuery() = 
            match this with
            | M1 -> "1min"
            | M5 -> "5min"
            | M15 -> "15min"
            | M30 -> "30min"
            | M60 -> "60min"
            | DAY -> "daily"
            | WEEK -> "weekly"
            | MONTH -> "monthly"


    module private Internals = 
        type AlphaVantageFunction = 
        | AVF_TIME_SERIES_DAILY of symbol:string
        | AVF_TIME_SERIES_INTRADAY of symbol:string * timeframe:Timeframe
        | AVF_FX_DAILY of cross:string 
        | AVF_FX_INTRADAY of cross:string * timeframe:Timeframe
        | AVF_SMA of symbol:string * timeframe:Timeframe * period:int * price:PriceBarPart

        let [<Literal>] domain = @"https://www.alphavantage.co"
        let [<Literal>] apiKey = "C0V5EYWW46O09787"
        let urlFormat = Printf.StringFormat<string->string>(domain + @"/query?%s&outputsize=full&apikey=" + apiKey)

        let buildUrl call = 
            let query = 
                let priceQuery price = 
                    match price with
                    | Close -> "close"
                    | Open -> "open"
                    | High -> "high"
                    | Low -> "low"
                match call with
                | AVF_TIME_SERIES_DAILY symbol -> sprintf "function=TIME_SERIES_DAILY&symbol=%s" symbol
                | AVF_TIME_SERIES_INTRADAY (symbol, timeframe) -> sprintf "function=TIME_SERIES_INTRADAY&symbol=%s&interval=%s" symbol (timeframe.ToQuery())
                | AVF_SMA (symbol, timeframe, period, price) -> sprintf "function=SMA&symbol=%s&interval=%s&time_period=%d&series_type=%s" symbol (timeframe.ToQuery()) period (priceQuery price)
                | _ -> sprintf "cll %A not managed yet" call |> failwith
            sprintf urlFormat query

        let extract jv = 
            match jv with
            | JsonValue.Record [|_metaTitle, JsonValue.Record [|_; _; _, serverTime; _; _|]; _seriesTitle, series|]
            | JsonValue.Record [|_metaTitle, JsonValue.Record [|_; _; _, serverTime; _; _; _|]; _seriesTitle, series|] 
            | JsonValue.Record [|_metaTitle, JsonValue.Record [|_; _; _, serverTime; _; _; _; _|]; _seriesTitle, series|]
            | JsonValue.Record [|_metaTitle, JsonValue.Record [|_; _; _; _, serverTime; _|]; _seriesTitle, series|] when fst (DateTime.TryParse(serverTime.AsString())) -> 
                serverTime.AsDateTime(),
                match series with
                | JsonValue.Record quotes -> quotes
                | _ -> failwith " Unrecognized JSON format"
            | _ -> failwith " Unrecognized JSON format"

        let json2Instrument (dt, jvq) : InstrumentMarketDataElement = 
            match jvq with
            | JsonValue.Record [| _,sO; _,sH; _,sL; _,sC; _, sV|] -> (DateTime.Parse(dt), {O = sO.AsFloat(); H = sH.AsFloat(); L = sL.AsFloat(); C = sC.AsFloat(); V = sV.AsInteger()})
            | _ -> failwith " Unrecognized JSON format"

        let json2Indicator (dt, jvq) : IndicatorMarketDataElement = 
            match jvq with
            | JsonValue.Record [|_,sV|] -> (DateTime.Parse(dt), sV.AsFloat())
            | _ -> failwith " Unrecognized JSON format"

        let exec parser call = async {
            let url = buildUrl call
            let! jv = JsonValue.AsyncLoad(url)    
            let dt, series = extract jv
            return series |> Seq.map parser
        }

    open Internals

    let getStockAsync symbol timeframe = async {
        let! marketData = 
            match timeframe with
            | M1 | M5 | M15 | M30 | M60 -> AVF_TIME_SERIES_INTRADAY (symbol, timeframe)
            | _ -> AVF_TIME_SERIES_DAILY symbol
            |> exec json2Instrument

        return {Symbol = symbol; MarketData = marketData} |> Instrument
    }

    let getStock symbol timeframe = getStockAsync symbol timeframe |> Async.RunSynchronously

    let getIndiAsync symbol timeframe indicator = async {
        let! marketData = 
            match indicator with
            | SMA (period, price) -> AVF_SMA (symbol, timeframe, period, price)
            |> exec json2Indicator
        
        return { Instrument = symbol; Definition = indicator; MarketData = marketData } |> Indicator
    }

    let getIndi symbol timeframe indicator = getIndiAsync symbol timeframe indicator |> Async.RunSynchronously

