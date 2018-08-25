; Sample x64 Assembly Program

EXTERN P1 : PROC
EXTERN log : PROC

.data
realVal REAL8 +1.5 ; this stores a real number in 8 bytes
P05 REAL8 0.5
SGND QWORD 08000000000000000h

align 16 
b0 REAL8 0.2316419, 0.2316419
b1 REAL8 0.319381530, 0.319381530
b2 REAL8 -0.356563782, -0.356563782
b3 REAL8 1.781477937, 1.781477937
b4 REAL8 -1.821255978, -1.821255978
b5 REAL8 1.330274429, 1.330274429

lnC0 REAL8 -1.941064448, -1.941064448
lnC1 REAL8 3.529305040, 3.529305040
lnC2 REAL8 -2.461222169, -2.461222169
lnC3 REAL8 1.130626210, 1.130626210
lnC4 REAL8 -0.2887399591, -0.2887399591
lnC5 REAL8 0.03110401824, 0.03110401824
lnM1P1 REAL8 1.0, -1.0

align 16
M05 REAL8 -0.5, -0.5, -0.5, -0.5
one REAL8 1.0, 1.0, 1.0, 1.0

one_div_sqrt_of_2pi REAL8 0.3989422804, 0.3989422804
dummy1 REAL8 0.0
STRIKE REAL8 0.0

L2E REAL8 1.44269504088896340735992468100, 1.44269504088896340735992468100, 1.44269504088896340735992468100, 1.44269504088896340735992468100
expC0 REAL8 0.3371894346, 0.3371894346, 0.3371894346, 0.3371894346
expC1 REAL8 0.657636276, 0.657636276, 0.657636276, 0.657636276
expC2 REAL8 1.00172476, 1.00172476, 1.00172476, 1.00172476
expA1 REAL8 0.6931471805599453094172321214, 0.6931471805599453094172321214, 0.6931471805599453094172321214, 0.6931471805599453094172321214
expA2 REAL8 0.2402265069591007123335512631, 0.2402265069591007123335512631, 0.2402265069591007123335512631, 0.2402265069591007123335512631
expA3 REAL8 0.0555041086648215799531422637, 0.0555041086648215799531422637, 0.0555041086648215799531422637, 0.0555041086648215799531422637
CNVI QWORD 4338000000000000h, 4338000000000000h, 4338000000000000h, 4338000000000000h


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

exp PROC ;argument and result in ST(0)
	FLDL2E
	FMUL
	FLD ST
	FRNDINT
	FXCH
	FSUB ST, ST(1)
	F2XM1
	FLD1
	FADDP ST(1), ST
	FSCALE
	FSTP ST(1)
	RET
exp ENDP

exp1 PROC
	VBROADCASTSD YMM16, L2E
	VBROADCASTSD YMM17, expC0
	VBROADCASTSD YMM18, expC1
	VBROADCASTSD YMM19, expC2

	RET
exp1 ENDP

ln PROC
	FLD1
	FLDL2E
	FDIVP
	FXCH
	FYL2X
	RET
ln ENDP

normalPDF PROC
	;one_div_sqrt_of_2pi * exp(-0.5 * x * x);
	;FLD ST
	;FMULP
	FMUL ST, ST	;STs = x * x
	FMUL M05	;STs = -0.5 * x * x
	CALL exp	;exp(-0.5 * x * x)
	;FLD one_div_sqrt_of_2pi
	;FMULP 
	FMUL one_div_sqrt_of_2pi
	RET
normalPDF ENDP

normalCDF PROC
	FLDZ		;STs = 0, x
	FCOMI ST, ST(1)
	JNA IsGE0
;	}
;	else {
;		return 1.0 - normalCDF(-x);
;	}
	FXCH		;STs = x, 0
	FSUBP		;STs = -x
	CALL Calcu	;STs = normalCDF(-x)
	FLD1		;STs = 1, normalCDF(-x)
	FXCH		;STs = normalCDF(-x), 1
	FSUBP		;STs = 1.0 - normalCDF(-x);
	JMP	nCDFEnd
IsGE0:			;STs = 0, x
;	if (x >= 0.0) {
	FSTP ST		;STs = x
	CALL Calcu	;STs = normalCDF(x)
	JMP	nCDFEnd
ChKGE0:

Calcu: ;STs = (-)x
;		double sigma = normalPDF(x);
	FLD ST		;STs = x, x
	CALL normalPDF ;STs = sigma, x
	FXCH		;STs = x, sigma
;		double t = 1. / (1. + b0 * x);
	FMUL b0		;STs = b0 * x, sigma
	FLD1		;STs = 1, b0 * x, sigma
	FADD ST(1),ST;STs = 1, 1 + b0 * x, sigma
	FXCH		;STs = 1 + b0 * x, 1, sigma
	FDIVP		;STs = 1 / (1 + b0*x) = t, sigma

;		double factor = t * (b1 + t * (b2 + t * (b3 + t * (b4 + t * b5))));
	FLD	ST			;STs = t, t, sigma
	FMUL b5			;STs = b5 * t, t, sigma
	FADD b4			;STs = b4 + b5 * t, t, sigma
	FMUL ST, ST(1)	;STs = t * (b4 + b5 * t), t, sigma
	FADD b3			;STs = b3 + t * (b4 + b5 * t), t, sigma
	FMUL ST, ST(1)	;STs = t * (b3 + t *(b4 + b5 * t)), t, sigma
	FADD b2			;STs = b2 + t * (b3 + t *(b4 + b5 * t)), t, sigma
	FMUL ST, ST(1)	;STs = t * (b2 + t * (b3 + t *(b4 + b5 * t))), t, sigma
	FADD b1			;STs = b1 + t * (b2 + t * (b3 + t *(b4 + b5 * t))), t, sigma
	FMULP			;STs = t * (b1 + t * (b2 + t * (b3 + t *(b4 + b5 * t)))) = factor, sigma

;		return 1.0 - sigma * factor;
	FMULP			;STs = factor * sigma
	FLD1			;STs = 1, factor * sigma
	FXCH			;STs = factor * sigma, 1
	FSUBP			;STs = 1 - factor * sigma
	RET

nCDFEnd:
	RET
normalCDF ENDP

nd12 PROC
	;double sigmaSqrHalved = volatility * volatility * 0.5;
	;double sigmaSqrT = volatility * pow(timeToMaturity, 0.5);
	VSQRTSD XMM5, XMM0, XMM4		;XMM5 = SQRT(timeToMaturity)
	VMULSD XMM6, XMM3, P05			;XMM6 = volatility * 0.5
	VUNPCKLPD XMM5, XMM5, XMM6		;XMM5 = (volatility * 0.5, SQRT(timeToMaturity))
	MULPD XMM5, XMM3				;XMM5 = (sigmaSqrHalved, sigmaSqrT)
	VSHUFPD XMM6, XMM5, XMM5, 3		;XMM6 = (sigmaSqrHalved, sigmaSqrHalved)
	VSHUFPD XMM5, XMM5, XMM5, 0		;XMM5 = (sigmaSqrT, sigmaSqrT)

	;double logSdivK = log(principal / strike);
	
	;VDIVSD	XMM7, XMM0, XMM1		;XMM7 = principal / strike
	;MOVQ MMWORD PTR [RSP-8h], XMM7
	;FLD	MMWORD PTR [RSP-8h]
	;CALL ln
	;FSTP MMWORD PTR [RSP-8h]
	;MOVQ XMM7, MMWORD PTR [RSP-8h]	;XMM7 = logSdivK
	;VUNPCKLPD XMM7, XMM7, XMM7		;XMM7.HI = XMM7.LO = logSdivK

	VMOVAPD XMM8, XMM0
	VMOVAPD XMM9, XMM1
	VMOVAPD XMM10, XMM2
	VMOVAPD XMM11, XMM3
	VMOVAPD XMM12, XMM4
	VMOVAPD XMM13, XMM5
	VDIVSD	XMM0, XMM0, XMM1		;XMM0 = principal / strike
	CALL log
	VMOVAPD XMM7, XMM0
	VMOVAPD XMM0, XMM8
	VMOVAPD XMM1, XMM9
	VMOVAPD XMM2, XMM10
	VMOVAPD XMM3, XMM11
	VMOVAPD XMM4, XMM12
	VMOVAPD XMM5, XMM13




	;d1, d2 = ((logSdivK + (rate + sigmaSqrHalved) * timeToMaturity) / sigmaSqrT, (logSdivK + (rate - sigmaSqrHalved) * timeToMaturity) / sigmaSqrT);
	VADDSUBPD XMM8, XMM2, XMM6		;XMM8 = (rate + sigmaSqrHalved, rate - sigmaSqrHalved)
	MULPD XMM8, XMM4				;XMM8 *= (timeToMaturity, timeToMaturity)
	ADDPD XMM8, XMM7				;XMM8 += (logSdivK, logSdivK)
	DIVPD XMM8, XMM5				;XMM8 /= (sigmaSqrT, sigmaSqrT) -> (d1, d2)

	;d1, d2 -> nd1, nd2
	SUB	RSP, 10h
	MOVUPD xmmword ptr [RSP], XMM8
	FLD	MMWORD PTR [RSP]			;STs = d1
	CALL normalCDF					;STs = normalCDF(d1) = nd1
	FSTP MMWORD PTR [RSP]
	FLD	MMWORD PTR [RSP+8];			;STs = d2
	CALL normalCDF					;STs = normalCDF(d2) = nd2
	FSTP MMWORD PTR [RSP+8]
	MOVUPD XMM5, xmmword ptr [RSP]	;return nd1, nd2 in XMM5
	ADD	RSP, 10h
	RET
nd12 ENDP

MKexpMinusRT PROC
;	return strike * exp(-rate * timeToMaturity);
	SUB	RSP, 8
	FLDZ					;STs = 0
	MOVQ MMWORD PTR [RSP], XMM2	
	FSUB MMWORD PTR [RSP]	;STs = -rate
	MOVQ MMWORD ptr [RSP], XMM4
	FMUL MMWORD PTR [RSP]	;STs = -rate * timeToMaturity
	CALL exp				;STs = exp(-rate * timeToMaturity)
	FLDZ					;STs = 0, exp(-rate * timeToMaturity)
	MOVQ MMWORD ptr [RSP], XMM1
	FSUB MMWORD PTR [RSP]	;STs = -strike, exp(-rate * timeToMaturity)
	FMUL					;STs = -strike * exp(-rate * timeToMaturity)
	FSTP MMWORD PTR [RSP]
	MOVQ XMM6, MMWORD ptr [RSP]
	ADD	RSP, 8
	RET
MKexpMinusRT ENDP

CallPrice1 PROC
	MOVQ XMM4, MMWORD PTR [RSP+28h]		;XMM4 = timeToMaturity
	VUNPCKLPD XMM2, XMM2, XMM2			;XMM2.HI = XMM2.LO = rate
	VUNPCKLPD XMM3, XMM3, XMM3			;XMM3.HI = XMM3.LO = volatility
	VUNPCKLPD XMM4, XMM4, XMM4			;XMM4.HI = XMM4.LO = timeToMaturity
	CALL nd12							;XMM5 = (nd1, nd2)
	CALL MKexpMinusRT					;XMM6 = -KexpMinusRT
	VUNPCKLPD XMM6, XMM6, XMM0			;XMM6 = (principal, -KexpMinusRT)
	VDPPD XMM0, XMM5, XMM6, 033h
	;double callValue = principal * nd1 - KexpMinusRT * nd2;
	RET
CallPrice1 ENDP

PUBLIC CallPrice
CallPrice PROC
	VSHUFPD XMM8, XMM0, XMM1, 0			;XMM8 = (strike, principal)
	VSHUFPD XMM9, XMM1, XMM0, 0			;XMM9 = (principal, strike)
	VDIVPD XMM10, XMM8, XMM9			;XMM10 = (strike/principal, principal/strike)
	MOVAPD XMM11, lnC5					;XMM10 = [c5]
	VFMADD213PD XMM11, XMM10, lnC4		;XMM11 = [c5*x+c4]
	VFMADD213PD XMM11, XMM10, lnC3		
	VFMADD213PD XMM11, XMM10, lnC2		
	VFMADD213PD XMM11, XMM10, lnC1		
	VFMADD213PD XMM11, XMM10, lnC0		;XMM11 = (ln(strike/principal), ln(principal/strike))
	VMULPD XMM11, XMM11, lnM1P1			;XMM11 = (-ln(strike/principal), ln(principal/strike))
	VPERMILPD XMM12, XMM11, 1			;XMM12 = (_, -ln(strike/principal))
	VCMPPD XMM13, XMM0, XMM1, 0			;XMM13 = principal >= strike
	VBLENDVPD XMM5, XMM11, XMM12, XMM13

		comment @

	;temporary (need to backup because library log resets XMM0 to XMM5)
	VMOVAPD XMM8, XMM0
	VMOVAPD XMM9, XMM1
	VMOVAPD XMM10, XMM2
	VMOVAPD XMM11, XMM3
	VDIVSD	XMM0, XMM0, XMM1			;XMM0 = principal / strike
	SUB RSP, 8 ;library log needs aligned stack

	;CALL log							;XMM0 = ln(principal / strike)
	XORPD XMM0, XMM0

	ADD RSP, 8
	VMOVAPD XMM5, XMM0					;XMM5 = ln(principal / strike) = logSdivK
	VMOVAPD XMM0, XMM8					;XMM0 = principal
	VMOVAPD XMM1, XMM9					;XMM1 = strike
	VMOVAPD XMM2, XMM10					;XMM2 = rate
	VMOVAPD XMM3, XMM11					;XMM3 = volatility
	;previous code to be replaced when inline log implemented
	   

	   @

	VBROADCASTSD YMM2, XMM2			;YMM2 = (rate, rate, rate, rate)
	VUNPCKLPD XMM3, XMM3, XMM3		;XMM3 = (volatility, volatility)
	VUNPCKLPD XMM5, XMM5, XMM5		;XMM5 = (logSdivK, logSdivK)
	VBROADCASTSD YMM4, QWORD PTR [RSP+28h] ;YMM4 = (timeToMaturity, timeToMaturity, timeToMaturity, timeToMaturity) 
	;MOVQ XMM4, MMWORD PTR [RSP+28h]	;XMM4 = timeToMaturity

	SQRTSD XMM8, XMM4				;XMM8 = SQRT(timeToMaturity)
	VMULSD XMM9, XMM3, P05			;XMM9 = volatility * 0.5
	VUNPCKLPD XMM8, XMM8, XMM9		;XMM8 = (volatility * 0.5, SQRT(timeToMaturity))
	MULPD XMM8, XMM3				;XMM8 = (sigmaSqrHalved, sigmaSqrT)
	VSHUFPD XMM9, XMM8, XMM8, 3		;XMM9 = (sigmaSqrHalved, sigmaSqrHalved)
	VSHUFPD XMM10, XMM8, XMM8, 0	;XMM10 = (sigmaSqrT, sigmaSqrT)
	VADDSUBPD XMM8, XMM2, XMM9		;XMM8 = (rate + sigmaSqrHalved, rate - sigmaSqrHalved)
	MULPD XMM8, XMM4				;XMM8 *= (timeToMaturity, timeToMaturity)
	ADDPD XMM8, XMM5				;XMM8 += (logSdivK, logSdivK)
	VDIVPD XMM6, XMM8, XMM10		;XMM6 /= (sigmaSqrT, sigmaSqrT) -> (d1, d2)

	VBROADCASTSD YMM8, SGND
	VANDNPD XMM7, XMM8, XMM6		;XMM7 = (|d1|, |d2|)
	MOVAPD XMM9, XMM7				;XMM9 = (|d1|, |d2|)
	MOVAPD XMM15, one				;XMM15 = [1]
	MULPD XMM9, b0					;XMM9 = [b0*|d|]
	ADDPD XMM9, XMM15				;XMM9 = [1+b0*|d|]
	VDIVPD XMM9, XMM15, XMM9		;XMM9 = [t]
	MOVAPD XMM10, b5				;XMM10 = [b5]
	VFMADD213PD XMM10, XMM9, b4		;XMM10 = [b5*t+b4]
	VFMADD213PD XMM10, XMM9, b3		;XMM10 = [(b5*t+b4)*t+b3]
	VFMADD213PD XMM10, XMM9, b2		;XMM10 = [((b5*t+b4)*t+b3)*t+b2]
	VFMADD213PD XMM10, XMM9, b1		;XMM10 = [(((b5*t+b4)*t+b3)*t+b2)*t+b1]
	MULPD XMM10, XMM9				;XMM10 = [((((b5*t+b4)*t+b3)*t+b2)*t+b1)*t]

	VORPD YMM11, YMM8, YMM2			;YMM11 = [-rate]
	VMULPD YMM11, YMM11, YMM4		;YMM11 = [-rate*timeToMaturity]
	VMULPD XMM12, XMM7, XMM7		;XMM12 = (|d1|^2,|d2|^2)
	MULPD XMM12, M05				;XMM12 = (-0.5*|d1|^2,-0.5*|d2|^2)
	MOVAPD XMM11, XMM12				;XMM11 = (-0.5*|d1|^2,-0.5*|d2|^2)

	;VMOVAPD YMM11, one
	;exp ;YMM11 = (exp(-rate*timeToMaturity),exp(-rate*timeToMaturity),exp(-0.5*|d1|^2),exp(-0.5*|d2|^2))
	comment @
	VMULPD YMM11, YMM11, L2E		;YMM11 = [log2(e) * x = t]	
	VROUNDPD YMM4, YMM11, 1			;YMM4 = [floor(t)]
	VMOVAPD YMM12, CNVI
	VADDPD YMM13, YMM12, YMM4
	VPSUBQ YMM12, YMM13, YMM12		;YMM12 = [i = (int)floor(t)]
	VSUBPD YMM13, YMM11, YMM4		;YMM13 = [f = t - floor(t)]
	VMOVAPD YMM4, expA3				;YMM4 = [a3]
	VFMADD213PD YMM4, YMM13, expA2	;YMM4 = [a2+a3*f]
	VFMADD213PD YMM4, YMM13, expA1	;YMM4 = [a1+a2*f+a3*f2]
	VFMADD213PD YMM4, YMM13, one	;YMM4 = [1+a1*f+a2*f2+a3*f3 ~ 2^f]
	VPSLLQ YMM12, YMM12, 52
	VPADDQ YMM11, YMM12, YMM4
	@

	VMULPD YMM11, YMM11, L2E		;YMM11 = [log2(e) * x = t]	
	VROUNDPD YMM4, YMM11, 1			;YMM4 = [floor(t)]
	VMOVUPD YMM12, CNVI
	VADDPD YMM13, YMM12, YMM4
	VPSUBQ YMM12, YMM13, YMM12		;YMM12 = [i = (int)floor(t)]
	VSUBPD YMM13, YMM11, YMM4		;YMM13 = [f = t - floor(t)]
	VMOVUPD YMM4, expC0				;YMM4 = [c0]
	VFMADD213PD YMM4, YMM13, expC1	;YMM4 = [c0*f+c1]
	VFMADD213PD YMM4, YMM13, expC2	;YMM4 = [(c0*f+c1)*f+c2 ~ 2^f]
	VPSLLQ YMM12, YMM12, 52
	VPADDQ YMM11, YMM12, YMM4

	XORPD XMM12, XMM12										;reset XMM12
	SUBPD XMM12, XMM1										;XMM12 = [-strike]
	MOVSD STRIKE, XMM12										;store -strike
	VMULPD YMM11, YMM11, YMMWORD PTR [one_div_sqrt_of_2pi]	;YMM11 = (-strike*exp(-rate*timeToMaturity)=-KexpMinusRT,0,sigma(|d1|), sigma(|d2|))
	MULPD XMM11, XMM10										;XMM11 = (factor*sigma(|d1|), factor*sigma(|d2|))
	VSUBPD XMM12, XMM15, XMM11								;XMM12 = (1-factor*sigma(|d1|), 1-factor*sigma(|d2|))
	VBLENDVPD XMM13, XMM12, XMM11, XMM6						;XMM13 = (nd1, nd2)
	VPERM2F128 YMM14, YMM11, YMM11, 1						;YMM14 = (_, _, -KexpMinusRT, 0)
	ADDPD XMM14, XMM0										;XMM14 = (-KexpMinusRT, principal)
	SHUFPD XMM14, XMM14, 1									;XMM14 = (principal, -KexpMinusRT)
	VDPPD XMM0, XMM13, XMM14, 031h							;XMM0 = principal * nd1 - _KexpMinusRT * nd2 = RESULT
	RET
CallPrice ENDP
END