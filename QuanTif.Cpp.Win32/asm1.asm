; Sample x64 Assembly Program
.data
realVal REAL8 +1.5 ; this stores a real number in 8 bytes

.code
PUBLIC CombineA
CombineA PROC
   ADD    ECX, DWORD PTR [RSP+28H] ; add overflow parameter to first parameter
   ADD    ECX, R9D                 ; add other three register parameters
   ADD    ECX, R8D                 ;
   ADD    ECX, EDX                 ;
   MOVD   XMM0, ECX                ; move doubleword ECX into XMM0
   CVTDQ2PD  XMM0, XMM0            ; convert doubleword to floating point
   MOVSD  XMM1, realVal            ; load 1.5
   ADDSD  XMM1, MMWORD PTR [RSP+30H]  ; add parameter
   DIVSD  XMM0, XMM1               ; do division, answer in xmm0
   RET                             ; return
CombineA ENDP
End
