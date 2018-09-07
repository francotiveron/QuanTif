#r @"bin\Debug\DiffSharp.dll"
#r @"bin\Debug\Hype.dll"

open Hype
open DiffSharp.AD.Float32

let dt = D 0.1f
let x0 = toDV [0.; 8.]
let v0 = toDV [0.75; 0.]

let p w (x:DV) = (1.f / DV.norm (x - toDV [D 10.f + w * D 0.f; D 10.f - w])) 
               + (1.f / DV.norm (x - toDV [10.; 0.]))

let trajectory (w:D) = 
    (x0, v0) 
    |> Seq.unfold (fun (x, v) ->
                    let a = -grad (p w)  x
                    let v = v + dt * a
                    let x = x + dt * v
                    Some(x, (x, v)))
    |> Seq.takeWhile (fun x -> x.[1] > D 0.f)

let error (w:DV) =
    let xf = trajectory w.[0] |> Seq.last
    xf.[0] * xf.[0]

//let w, l, whist, lhist = Optimize.Minimize(error, toDV [0.], 
//                                            {Params.Default with 
//                                                Method = Newton; 
//                                                LearningRate = Constant (D 1.f)
//                                                ValidationInterval = 1;
//                                                Epochs = 10})

let w, l, whist, lhist = Optimize.Minimize(error, toDV [0.], 
                                            {Params.Default with 
                                                LearningRate = Constant (D 1.f)
                                                ValidationInterval = 1;
                                                Epochs = 10})
