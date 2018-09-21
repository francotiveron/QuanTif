open System.Runtime.InteropServices
[<DllImport(@"C:\Root\Project\QuanTif\x64\Debug\QuanTif.x64.exe", CallingConvention = CallingConvention.Cdecl)>]
extern float CombineA(float x)
[<DllImport(@"C:\Root\Project\QuanTif\x64\Debug\QuanTif.x64.exe", CallingConvention = CallingConvention.Cdecl)>]
extern float CallPrice(float principal, float strike, float rate, float volatility, float timeToMaturity)
CombineA(1.)
CallPrice(100., 100., 0.05, 0.2, 1.0)