#I @"QuanTif.IO\bin\Debug\"
#r @"QuanTif.IO\bin\Debug\XPlot.Plotly.dll"
#r @"QuanTif.Common\bin\Debug\QuanTif.Common.dll"
#r @"QuanTif.IO\bin\Debug\QuanTif.IO.dll"
#r @"QuanTif.Feed\bin\Debug\QuanTif.Feed.dll"
#r @"QuanTif.Trading\bin\Debug\Deedle.dll"
#r @"QuanTif.Trading\bin\Debug\QuanTif.Trading.dll"

open QuanTif.Common
open QuanTif.Feed.AlphaVantage

#time
let msft = getStock "MSFT" DAY
#time
printf "%A" msft

