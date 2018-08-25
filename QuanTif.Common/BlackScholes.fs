namespace QuanTif.Common
#nowarn "0025"

open System

module BlackScholes =
    let private one_div_sqrt_of_2pi = 1.0 / Math.Sqrt(2.0 * Math.PI)

    let private normalPDF x =  one_div_sqrt_of_2pi * Math.Exp(-0.5 * x * x)

    let b0, b1, b2, b3, b4, b5 = 0.2316419, 0.319381530, -0.356563782, 1.781477937, -1.821255978, 1.330274429
    let rec private normalCDF x =
        if x >= 0. then
            let t = 1. / (1. + b0 * x)
            let sigma = normalPDF(x)
            let factor = t * (b1 + t * (b2 + t * (b3 + t * (b4 + t * b5))))
            1.0 - sigma * factor
        else
            1.0 - (normalCDF -x)

    let internal d12 principal strike rate volatility timeToMaturity = 
        let logSdivK = Math.Log(principal / strike)
        let sigmaSqrHalved = volatility * volatility * 0.5
        let sigmaSqrT = volatility * Math.Sqrt(timeToMaturity)
        [(logSdivK + (rate + sigmaSqrHalved) * timeToMaturity) / sigmaSqrT;
        (logSdivK + (rate - sigmaSqrHalved) * timeToMaturity) / sigmaSqrT]

    let internal KexpMinusRT strike rate timeToMaturity = strike * Math.Exp(-rate * timeToMaturity)

    let internal calcOptions principal strike rate volatility timeToMaturity withPut = 
        let [nd1; nd2] = d12 principal strike rate volatility timeToMaturity |> List.map normalCDF
        let KexpMinusRT = KexpMinusRT strike rate timeToMaturity
        let callValue = principal * nd1 - KexpMinusRT * nd2
        if withPut then (callValue, KexpMinusRT - principal + callValue) else (callValue, 0.)

    let internal callPrice1 principal strike rate volatility timeToMaturity = 
        let [nd1; nd2] = d12 principal strike rate volatility timeToMaturity |> List.map normalCDF
        let KexpMinusRT = KexpMinusRT strike rate timeToMaturity
        principal * nd1 - KexpMinusRT * nd2

    let callPrice principal strike rate volatility timeToMaturity = 
        fst <| calcOptions principal strike rate volatility timeToMaturity false

    let putPrice principal strike rate volatility timeToMaturity = 
        snd <| calcOptions principal strike rate volatility timeToMaturity true

    let optionsPrices principal strike rate volatility timeToMaturity = 
        calcOptions principal strike rate volatility timeToMaturity true

module BlackScholesPerformance =
    open MathNet.Numerics.Distributions
    open QuanTifCppCLI
    open System.Runtime.InteropServices

    [<DllImport(@"C:\Root\Project\QuanTif\x64\Debug\QuanTif.x64.exe", CallingConvention = CallingConvention.Cdecl)>]
    extern float CallPrice(float principal, float strike, float rate, float volatility, float timeToMaturity)
    [<DllImport(@"C:\Root\Project\QuanTif\x64\Debug\QuanTif.x64.exe", CallingConvention = CallingConvention.Cdecl)>]
    extern float CallPriceRep(float principal, float strike, float rate, float volatility, float timeToMaturity, int repeat)

    type Mode = | FSharp | MathNET | CPP | CPPREP | ASM | ASMREP

    let private normalCDF x = Normal.CDF(0., 1., x)

    let private cpp = BlackScholesCppCLI()

    let internal callPrice principal strike rate volatility timeToMaturity = 
        let [nd1; nd2] = BlackScholes.d12 principal strike rate volatility timeToMaturity |> List.map normalCDF
        let KexpMinusRT = BlackScholes.KexpMinusRT strike rate timeToMaturity
        principal * nd1 - KexpMinusRT * nd2

    let optionsPricesStress principal strike rate volatility timeToMaturity repeat mode = 
        let n, calculator = 
            match mode with
            | FSharp -> repeat, fun() -> BlackScholes.callPrice1 principal strike rate volatility timeToMaturity
            | MathNET -> repeat, fun() -> callPrice principal strike rate volatility timeToMaturity
            | CPP -> repeat, fun() -> cpp.CallPrice(principal, strike, rate, volatility, timeToMaturity)
            | CPPREP -> 1, fun() -> cpp.CallPriceStress(principal, strike, rate, volatility, timeToMaturity, repeat)
            | ASM -> repeat, fun() -> CallPrice(principal, strike, rate, volatility, timeToMaturity)
            | ASMREP -> 1, fun() -> CallPriceRep(principal, strike, rate, volatility, timeToMaturity, repeat)
        
        let rec loop i =
            match i with
            | 1 -> calculator()
            | j -> calculator() |> ignore; loop <| j - 1
        loop n
        //for i in [2..n] do calculator() |> ignore
        //calculator()
