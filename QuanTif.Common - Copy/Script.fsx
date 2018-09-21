#r @"C:\Root\Project\QuanTif\Debug\QuanTif.Cpp.CLI.dll"
open QuanTifCppCLI

let cpp = BlackScholesCppCLI()
let i = cpp.Public_Function()
let j = cpp.Prole()

open System.Runtime.InteropServices

////[<DllImport(@"C:\Root\Project\QuanTif\Debug\QuanTif.Cpp.Win32.dll", CallingConvention = CallingConvention.Cdecl)>]
//[<DllImport(@"C:\Root\Project\QuanTif\x64\Debug\QuanTif.Cpp.Win32.dll", CallingConvention = CallingConvention.Cdecl)>]
//extern float Exp_Asm(float x)
//Exp_Asm(3.)

