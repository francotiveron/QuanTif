#r @"QuanTif.Common\bin\Debug\QuanTif.Common.dll"
#r @"QuanTif.Feed\bin\Debug\QuanTif.Feed.dll"
#r @"QuanTif.ML\bin\Debug\QuanTif.ML.dll"
#r @"QuanTif.ML\bin\Debug\Deedle.dll"
#r @"QuanTif.ML\bin\Debug\DiffSharp.dll"
#r @"QuanTif.ML\bin\Debug\Hype.dll"

open QuanTif.Common
open QuanTif.Feed.AlphaVantage
open QuanTif.ML
open Deedle
open System
open DiffSharp.AD.Float32
open Hype.Neural
open Hype
open DiffSharp
open DiffSharp.Util


let msft = getStock "MSFT" DAY
let toFrame md = 
    //let flatten (dt:DateTime, ohlcv:OHLCV) = [dt; ohlcv.O]
        
    match md with
    | Instrument data -> [data.Symbol, data.MarketData |> series] |> frame |> Frame.expandAllCols 2
    | _ -> failwith "No Ins"

let fr = toFrame msft
let r1 : Series<string, float> = fr.GetRows().ValuesAll |> Seq.head 
let r2 = fr.GetRows().ValuesAll |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32 |> toDV)
let v = r1.Values |> Seq.take 4 |> Seq.map float32 |> toDV
let r3 n m = fr.GetRows().ValuesAll |> Seq.skip n |> Seq.take m |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32) |> Seq.concat |> toDV
let r4 = r3 10 20

let r5 : seq<Series<string, float>> = fr.GetRows().ValuesAll |> Seq.skip 10 |> Seq.take 20
let r6 = r5 |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32)
let r7 = r6 |> Seq.concat
let r8 = r7 |> toDV

let r9 = 
    fr.GetRows().ValuesAll |> Seq.skip 10 |> Seq.take 20
    |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32)
    |> Seq.concat
    |> toDV

let toDV1 n r = 
    let r5 : seq<Series<string, float>> = fr.GetRows().ValuesAll |> Seq.skip r |> Seq.take n
    r5 
    |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32)
    |> Seq.concat
//    |> toDV

toDV1 10 20

[0..10] |> List.map (toDV1 100) |> toDM

let toDV2 n r = 
    let r5 : seq<Series<string, float>> = fr.GetRows().ValuesAll |> Seq.skip r |> Seq.take (n + 1)
    let r6 = r5 |> Seq.map (fun s -> s.Values |> Seq.take 4 |> Seq.map float32)
    r6 |> Seq.take n |> Seq.concat, Seq.last r6

let a1, a2 = [0..9] |> List.map (toDV2 10) |> List.unzip
let m1, m2 = toDM a1 |> DM.transpose, toDM a2 |> DM.transpose

printf "%O" m1
printf "%O" m2

let dd = Dataset(m1, m2)
dd.X.ToString()

let n = FeedForward()
n.Add(Linear(40, 4))
n.ToStringFull()
n.Visualize()
let l = n.[0] :?> Linear
dd.X.GetCols() |> Seq.head
let c0 = dd.X.GetCols() |> Seq.head
let wr0 = l.W.GetRows() |> Seq.head
wr0 * c0
let y' = l.W * dd.X
y' - dd.Y


let par = {Params.Default with
            //Batch = Minibatch 10
            LearningRate = LearningRate.RMSProp(D 0.01f, D 0.9f)
            Loss = Quadratic
            Epochs = 10
            Batch = Stochastic
            ValidationInterval = 1
            //Silent = true       // Suppress the regular printing of training progress
            //ReturnBest = false
            } 

let loss, _ = Layer.Train(n, dd, par)

let m2p = n.Run m1
printf "%O" m2p
printf "%O" m2