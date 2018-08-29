namespace QuanTif.IO

open XPlot.Plotly
open QuanTif.Common

[<AutoOpen>]
module Default = 
    open System
    open QuanTif.Trading

    let private marketToTrace withCandlestick marketData = 
        match marketData with
        | Instrument {Symbol = symbol; MarketData = series} -> 
            if withCandlestick then
                Candlestick(
                    name = symbol
                    ,x = (series |> Seq.map (fun (dt, _) -> dt))
                    ,``open`` = (series |> Seq.map (fun (_, ohlcv) -> ohlcv.O))
                    ,high = (series |> Seq.map (fun (_, ohlcv) -> ohlcv.H))
                    ,low = (series |> Seq.map (fun (_, ohlcv) -> ohlcv.L))
                    ,close = (series |> Seq.map (fun (_, ohlcv) -> ohlcv.C))
                ) :> Trace
            else
                Scatter(
                    name = symbol
                    ,x = (series |> Seq.map (fun (dt, _) -> dt))
                    ,y = (series |> Seq.map (fun (_, ohlcv) -> ohlcv.C))
                ) :> Trace
        | Indicator {Definition = def; MarketData = series} -> 
            Scatter(
                name = def.Id
                ,x = (series |> Seq.map (fun (dt, v) -> dt))
                ,y = (series |> Seq.map (fun (dt, v) -> v))
            ) :> Trace


    let private marketsToTraces marketsData = marketsData |> Seq.map (marketToTrace (marketsData |> Seq.length = 1))
        //let toChart chartData = 
        //    chartData |> Seq.map (marketToTrace (chartData |> Seq.length = 1))// |> Chart.Plot

        //marketsData |> Seq.map marketToTrace// |> Chart.ShowAll
    
    let private reportToTrace report =
        Scatter(
            x = (report.Operations |> List.map (fun op -> op.Time))
            ,y = (report.Operations |> Seq.map (fun op -> op.Price))
            //,text = (result.operations.Values |> Seq.map (fun e -> e.shares))
            ,mode = "markers"
            ,marker = 
                Marker(
                    color = (report.Operations |> Seq.map (fun op -> if op.Shares > 0 then "green" else "red"))
                    ,size = 12
                )
        ) :> Trace


    type Shower =
        static member ($) (_:Shower, m:seq<#seq<MarketData>>) = m |> Seq.map marketsToTraces |> Seq.map Chart.Plot |> Chart.ShowAll
        static member ($) (_:Shower, r:BacktestReport) = reportToTrace r
        static member ($) (_:Shower, (r:BacktestReport, m:seq<MarketData>)) = seq { yield! marketsToTraces m; yield reportToTrace r } |> Chart.Plot |> Chart.Show


    let inline show o = Unchecked.defaultof<Shower> $ o