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

let msft = getStock "MSFT" DAY
let toFrame md =         
    match md with
    | Instrument data -> [data.Symbol, data.MarketData |> series] |> frame |> Frame.expandAllCols 2
    | _ -> failwith "No Ins"

let fr = toFrame msft

let toDataset nPrevDays = 
    let prevsAndSucc nPrevDays offset = //seq<o, h, l, c, o, h, l, c...>, seq<o, h, l, c>
        if offset % 10 = 0 then printfn "offset = %d" offset
        let rowsPrevsAndSucc : seq<Series<string, float>> = fr.GetRows().ValuesAll |> Seq.skip offset |> Seq.take (nPrevDays + 1)
        let ohlcPrevsAnsSucc = rowsPrevsAndSucc |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32)
        ohlcPrevsAnsSucc |> Seq.take nPrevDays |> Seq.concat, Seq.last ohlcPrevsAnsSucc
    let nRows = fr.RowCount - nPrevDays - 1
    printfn "nRows = %d" nRows
    //let Xseq, Yseq = [0..nRows] |> List.map (prevsAndSucc 10) |> List.unzip
    let Xseq, Yseq = [(nRows - 99)..nRows] |> List.map (prevsAndSucc nPrevDays) |> List.unzip
    let X, Y = toDM Xseq |> DM.transpose, toDM Yseq |> DM.transpose
    Dataset(X, Y)

let dset = toDataset 100
let n = FeedForward()
n.Add(Linear(400, 1000))
n.Add(Linear(1000, 300))
n.Add(Linear(300, 4))

let par = {Params.Default with
            //Batch = Minibatch 10
            LearningRate = LearningRate.RMSProp(D 0.01f, D 0.9f)
            Loss = Quadratic
            Epochs = 10
            Batch = Minibatch 10
            ValidationInterval = 1
            //Silent = true       // Suppress the regular printing of training progress
            //ReturnBest = false
            } 

n.Reset()
let loss, _ = Layer.Train(n, dset, par)

let Yp = n.Run dset.X
printf "%O" Yp
printf "%O" dset.Y