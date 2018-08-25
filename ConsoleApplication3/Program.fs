open QuanTif.Common.BlackScholesPerformance

[<EntryPoint>]
let main argv = 

    //optionsPricesStress 100. 100. 0.05 0.2 1.0 10000000 FSharp |> ignore
    //optionsPricesStress 100. 100. 0.05 0.2 1.0 1000 MathNET |> ignore
    //optionsPricesStress 100. 100. 0.05 0.2 1.0 1000 CPP |> ignore
    optionsPricesStress 100. 100. 0.05 0.2 1.0 10000000 CPPREP |> ignore
    0 // return an integer exit code
