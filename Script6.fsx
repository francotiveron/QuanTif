#r @"QuanTif.Common\bin\Debug\QuanTif.Common.dll"
#r @"QuanTif.Feed\bin\Debug\QuanTif.Feed.dll"
#r @"QuanTif.ML\bin\Debug\QuanTif.ML.dll"
#r @"QuanTif.ML\bin\Debug\Deedle.dll"
#r @"QuanTif.ML\bin\Debug\DiffSharp.dll"
#r @"QuanTif.ML\bin\Debug\Hype.dll"

open QuanTif.Common
open QuanTif.Feed.AlphaVantage
open Deedle
open DiffSharp.AD.Float32
open Hype.Neural
open Hype
open Hype

let msft = getStock "MSFT" DAY
let toFrame md =         
    match md with
    | Instrument data -> [data.Symbol, data.MarketData |> series] |> frame |> Frame.expandAllCols 2
    | _ -> failwith "No Ins"

let fr = toFrame msft

let toDataset nPrevDays n b = 
    let prevsAndSucc nPrevDays offset = //seq<o, h, l, c, o, h, l, c...>, seq<o, h, l, c>
        if offset % 10 = 0 then printfn "offset = %d" offset
        let rowsPrevsAndSucc : seq<Series<string, float>> = fr.GetRows().ValuesAll |> Seq.skip offset |> Seq.take (nPrevDays + 1)
        let ohlcPrevsAnsSucc = rowsPrevsAndSucc |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32)
        ohlcPrevsAnsSucc |> Seq.take nPrevDays |> Seq.concat, Seq.last ohlcPrevsAnsSucc
    let nRows = fr.RowCount - nPrevDays - 1
    printfn "nRows = %d" nRows
    let Xseq, Yseq = [(nRows - b - n + 1)..(nRows - b)] |> List.map (prevsAndSucc nPrevDays) |> List.unzip
    let X, Y = toDM Xseq |> DM.transpose, toDM Yseq |> DM.transpose
    Dataset(X, Y)

let dset = toDataset 200 5 0

let par = {Params.Default with
            //Batch = Minibatch 10
            //LearningRate = LearningRate.RMSProp(D 0.01f, D 0.9f)
            LearningRate = LearningRate.RMSProp(D 0.01f, D 0.9f)
            //LearningRate = LearningRate.DefaultRMSProp
            //EarlyStopping = EarlyStopping.DefaultEarly
            Loss = Loss.Quadratic
            Epochs = 5000
            Batch = Stochastic
            ValidationInterval = 10
            //Silent = true       // Suppress the regular printing of training progress
            //ReturnBest = false
            } 

let nn = FeedForward()
nn.Add(Linear(800, 200))
nn.Add(LSTM(200, 200))
nn.Add(Linear(200, 4))

nn.Reset()
let loss, _ = Layer.Train(nn, dset.GetSlice(Some 0, Some 0), par)


nn.Run dset.X.[*,0..0] |> printf "%O" 
dset.Y.[*,0..0] |> printf "%O" 

nn.Run dset.X.[*,1..1] |> printf "%O" 
dset.Y.[*,1..1] |> printf "%O" 

nn.Run dset.X.[*,2..2] |> printf "%O" 
dset.Y.[*,2..2] |> printf "%O" 

nn.Run dset.X |> printf "%O" 
dset.Y |> printf "%O" 

