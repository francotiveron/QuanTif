namespace QuanTif.Common

open System

type PriceBarPart = | Open | High | Low | Close
type OHLCV = { O:float; H:float; L:float; C:float; V:int }
type InstrumentMarketDataElement = DateTime * OHLCV
type InstrumentMarketData = InstrumentMarketDataElement seq
type IndicatorMarketDataElement = DateTime * float
type IndicatorMarketData = IndicatorMarketDataElement seq

type IndicatorDefinitions = 
    | SMA of period:int * price:PriceBarPart
with
    member this.Id = 
        match this with
        | SMA (period, price) -> sprintf "SMA%d%A" period price

type Instrument = {
    Symbol : string
    MarketData : InstrumentMarketData
}

type Indicator = {
    Instrument : string
    Definition : IndicatorDefinitions
    MarketData : IndicatorMarketData
}
with
    member this.Id = sprintf "%s(%s)" this.Definition.Id this.Instrument

type MarketData = 
    | Instrument of Instrument
    | Indicator of Indicator

