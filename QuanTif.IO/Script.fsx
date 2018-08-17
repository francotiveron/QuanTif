#r @"C:\Root\Project\QuanTif\packages\XPlot.Plotly.1.5.0\lib\net45\XPlot.Plotly.dll"

open XPlot.Plotly

let data =
    [
         // x,  o,  h,  l,  c   
        "Mon", 28, 45, 20, 38 
        "Tue", 38, 66, 31, 55 
        "Wed", 55, 80, 50, 77
        "Thu", 77, 77, 50, 66
        "Fri", 66, 68, 15, 22        
    ]
        
Chart.Candlestick data |> Chart.Show
let trace1 =
    Scatter(
        x = [1; 2; 3; 4],
        y = [10; 15; 13; 17]
    )

let trace2 =
    Scatter(
        x = [2; 3; 4; 5],
        y = [16; 5; 11; 9]
    )

[trace1; trace2]
|> Chart.Plot
|> Chart.WithWidth 700
|> Chart.WithHeight 500

let trace3 = Candlestick(``open`` = [1.0; 2.0], close = [2.0; 3.0], low = [0.0; 1.0], high = [3.0; 4.0], x = ["2018-08-08"; "2018-08-09"])
let trace4 = Scatter(x = ["2018-08-08"; "2018-08-09"], y = [2.5; 2.6])
let cha = Chart.Plot [trace3 :> Trace; trace4 :> Trace]