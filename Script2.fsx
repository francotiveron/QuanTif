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

let gets symbol = getStock symbol DAY
let geti symbol = getIndi symbol DAY

let msft = gets "MSFT" 
let aapl = gets "AAPL" 

let pickLast n m = 
    let last n seq' = seq' |> Seq.take n
    match m with
    | Instrument ins -> Instrument { ins with MarketData = last n ins.MarketData }
    | Indicator ind -> Indicator { ind with MarketData = last n ind.MarketData }

let sma8Msft = geti "MSFT" (SMA (8, Close))
let sma20Msft = geti "MSFT" (SMA (20, Close))
let sma50Msft = geti "MSFT" (SMA (50, Close))

let sma100Aapl = geti "AAPL" (SMA (100, Close))
let sma200Aapl = geti "AAPL" (SMA (200, Close))

show [[pickLast 50 msft]; [pickLast 100 aapl]]
show [[msft; sma8Msft; sma20Msft; sma50Msft]; [aapl; sma100Aapl; sma200Aapl]]

