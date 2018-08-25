#r @"bin\Debug\QuanTif.Common.dll"
open QuanTif.Common.BlackScholesPerformance

#time

optionsPricesStress 100. 100. 0.05 0.2 1.0 (1<<<26) FSharp
optionsPricesStress 100. 100. 0.05 0.2 1.0 (1<<<26) MathNET
optionsPricesStress 100. 100. 0.05 0.2 1.0 (1<<<26) CPP
optionsPricesStress 100. 100. 0.05 0.2 1.0 (1<<<26) CPPREP
optionsPricesStress 100. 100. 0.05 0.2 1.0 (1<<<26) ASM
optionsPricesStress 100. 100. 0.05 0.2 1.0 (1<<<26) ASMREP

let f1 n r m = 
    async {
        return optionsPricesStress 100. 100. 0.05 0.2 1.0 n m
    } |> Seq.replicate r |> Async.Parallel |> Async.RunSynchronously |> Seq.head

let f2 = f1 (1<<<24) 4


f2 FSharp
f2 MathNET
f2 CPP
f2 ASM
f2 CPPREP
f2 ASMREP
