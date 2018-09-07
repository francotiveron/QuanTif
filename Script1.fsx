#I @"QuanTif.IO\bin\Debug\"
#r @"QuanTif.IO\bin\Debug\XPlot.Plotly.dll"
#r @"QuanTif.Common\bin\Debug\QuanTif.Common.dll"
#r @"QuanTif.IO\bin\Debug\QuanTif.IO.dll"
#r @"QuanTif.Feed\bin\Debug\QuanTif.Feed.dll"
#r @"QuanTif.Trading\bin\Debug\Deedle.dll"
#r @"QuanTif.Trading\bin\Debug\QuanTif.Trading.dll"

open QuanTif.Common
open QuanTif.Feed.AlphaVantage
open QuanTif.IO
open QuanTif.Trading

let msft = getStock "MSFT" DAY
let geti = getIndi "MSFT" DAY
let sma10 = SMA (10, Close) |> geti
let sma20 = SMA (20, Close) |> geti
let sma50 = SMA (50, Close) |> geti

//show [[msft; sma20; sma50]; [msft]]

let strategy1 (st:IBacktestStatus) =
    match st.GetIndicator("SMA10Close(MSFT)", 0), st.GetIndicator("SMA20Close(MSFT)", 0), st.GetIndicator("SMA10Close(MSFT)", -1), st.GetIndicator("SMA20Close(MSFT)", -1) with
    | Some sma20, Some sma50, Some sma20p, Some sma50p ->
        let q = match st.GetPosition("MSFT") with | Some _ -> 200 | _ -> 100
        match sma20 - sma50, sma20p - sma50p with
        | diff, diffp when diff > 0. && diffp <= 0. -> st.Trade("MSFT", q) |> ignore
        | diff, diffp when diff < 0. && diffp >= 0. -> st.Trade("MSFT", -q) |> ignore
        | _ -> ()
    | _ -> ()

let strategy2 (st:IBacktestStatus) =
    match 
          st.GetIndicator("SMA20Close(MSFT)", 0)
        , st.GetIndicator("SMA50Close(MSFT)", 0)
        , st.GetIndicator("SMA20Close(MSFT)", -1)
        , st.GetIndicator("SMA50Close(MSFT)", -1) 
        with
    | Some sma20, Some sma50, Some sma20p, Some sma50p ->
        let q = match st.GetPosition("MSFT") with | Some _ -> 100 | _ -> 50
        match sma20 - sma50, sma20p - sma50p with
        | diff, diffp when diff > 0. && diffp <= 0. -> st.Trade("MSFT", q) |> ignore
        | diff, diffp when diff < 0. && diffp >= 0. -> st.Trade("MSFT", -q) |> ignore
        | _ -> ()
    | _ -> ()

//let report = 
//    {
//        Market = [msft; sma20; sma50]
//        Strategy = strategy
//    } |> Backtest.backtest

//show (report, [msft; sma20; sma50])

let market1 = [msft; sma10; sma20]
let market2 = [msft; sma20; sma50]
let report2 = {Market = market2; Strategy = strategy2} |> Backtest.backtest
show (report2, market2|> List.toSeq)

let test strategy market = 
    {Market = market; Strategy = strategy} |> Backtest.backtest

[strategy1, market1; strategy2, market2] |> List.map (fun p -> p ||> test, snd p |> List.toSeq) |> List.map show
//[strategy1, market1; strategy2, market2] |> List.map ((||>) >> test >> (fun rpt -> show (rpt, market)))