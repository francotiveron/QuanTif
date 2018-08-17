open QuanTif.AlphaVantage
open QuanTif.IO

[<EntryPoint>]
let main argv = 
    let msft = StockData.getBars Stock "MSFT" D |> Async.RunSynchronously
    let data = msft.Bars |> Array.map (fun bar -> (bar.T, bar.O, bar.H, bar.L, bar.C))
    Chart.chart data
    0 // return an integer exit code
