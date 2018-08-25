; Black Scholes Option Call Price 
.data
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
M05 REAL8 -0.5, -0.5, -0.5, -0.5
one REAL8 1.0, 1.0, 1.0, 1.0
one_div_sqrt_of_2pi REAL8 0.3989422804, 0.3989422804
dummy1 REAL8 0.0
STRIKE REAL8 0.0
L2E REAL8 1.44269504088896340735992468100, 1.44269504088896340735992468100, 1.44269504088896340735992468100, 1.44269504088896340735992468100

expC0 REAL8 0.3371894346, 0.3371894346, 0.3371894346, 0.3371894346
expC1 REAL8 0.657636276, 0.657636276, 0.657636276, 0.657636276
expC2 REAL8 1.00172476, 1.00172476, 1.00172476, 1.00172476

CNVI QWORD 4338000000000000h, 4338000000000000h, 4338000000000000h, 4338000000000000h

.code
PUBLIC CallPrice
CallPrice PROC

comment @ calculate log(principal{S}/strike{K})
	for i<=x<=2 ln(x)~L(x)=-1.941064448+(3.529305040+(-2.461222169+(1.130626210+(-0.2887399591+0.03110401824x)x)x)x)x
	assuming K in the range [S-50%, S+50%] , S/K range is [2/3, 2] and K/S range is [1/2, 3/2], hence
	if (S >= K) 
		take L(S/K) // 1 <= S/K <= 2
	else
		take -L(K/S) // 1 <= K/S <= 1.5	
@
	VSHUFPD XMM8, XMM0, XMM1, 0			;XMM8 = (K, S)
	VSHUFPD XMM9, XMM1, XMM0, 0			;XMM9 = (S, K)
	VDIVPD XMM10, XMM8, XMM9			;XMM10 = (K/S, S/K)
	MOVAPD XMM11, lnC5					;XMM10 = [c5]
	VFMADD213PD XMM11, XMM10, lnC4		;XMM11 = [c5*x+c4]
	VFMADD213PD XMM11, XMM10, lnC3		
	VFMADD213PD XMM11, XMM10, lnC2		
	VFMADD213PD XMM11, XMM10, lnC1		
	VFMADD213PD XMM11, XMM10, lnC0		;XMM11 = (L(K/S), L(S/K))
	VMULPD XMM11, XMM11, lnM1P1			;XMM11 = (-L(K/S), L(S/K))
	VPERMILPD XMM12, XMM11, 1			;XMM12 = (_, -L(K/S))
	VCMPPD XMM13, XMM0, XMM1, 0			;XMM13 = S >= K ?
	VBLENDVPD XMM5, XMM11, XMM12, XMM13 ;XMM5 ~ ln(S/K)

;broadcast inputs as needed for AVX vector calculation
	VBROADCASTSD YMM2, XMM2			;YMM2 = (rate, rate, rate, rate)
	VUNPCKLPD XMM3, XMM3, XMM3		;XMM3 = (volatility, volatility)
	VUNPCKLPD XMM5, XMM5, XMM5		;XMM5 = (logSdivK, logSdivK)
	VBROADCASTSD YMM4, QWORD PTR [RSP+28h] ;YMM4 = (timeToMaturity, timeToMaturity, timeToMaturity, timeToMaturity) 

comment @ calculate d1 d2
	translate C {
		double logSdivK = log(principal / strike);
		double sigmaSqrHalved = volatility * volatility * 0.5;
		double sigmaSqrT = volatility * sqrt(timeToMaturity);
		d1 = (logSdivK + (rate + sigmaSqrHalved) * timeToMaturity) / sigmaSqrT;
		d2 = (logSdivK + (rate - sigmaSqrHalved) * timeToMaturity) / sigmaSqrT;
	}
@
	SQRTSD XMM8, XMM4				;XMM8 = sqrt(timeToMaturity)
	VMULSD XMM9, XMM3, P05			;XMM9 = volatility * 0.5
	VUNPCKLPD XMM8, XMM8, XMM9		;XMM8 = (volatility * 0.5, sqrt(timeToMaturity))
	MULPD XMM8, XMM3				;XMM8 *= (volatility, volatility) = (sigmaSqrHalved, sigmaSqrT)
	VSHUFPD XMM9, XMM8, XMM8, 3		;XMM9 = (sigmaSqrHalved, sigmaSqrHalved)
	VSHUFPD XMM10, XMM8, XMM8, 0	;XMM10 = (sigmaSqrT, sigmaSqrT)
	VADDSUBPD XMM8, XMM2, XMM9		;XMM8 = (rate + sigmaSqrHalved, rate - sigmaSqrHalved)
	MULPD XMM8, XMM4				;XMM8 *= (timeToMaturity, timeToMaturity)
	ADDPD XMM8, XMM5				;XMM8 += (logSdivK, logSdivK)
	VDIVPD XMM6, XMM8, XMM10		;XMM6 /= (sigmaSqrT, sigmaSqrT) -> (d1, d2)

comment @ prepare for packed exponential
	translate C {
		double normalPDF(double x) {
			return one_div_sqrt_of_2pi * exp(-0.5 * x * x);
		}

		double normalCDF(double x) {
			if (x >= 0.0) {
				double t = 1. / (1. + b0 * x);
				double factor = t * (b1 + t * (b2 + t * (b3 + t * (b4 + t * b5))));
				double sigma = normalPDF(x);
				return 1.0 - sigma * factor;
			}
			else {
				return 1.0 - normalCDF(-x);
			}
		}

		double KexpMinusRT(double strike, double rate, double timeToMaturity) {
			return strike * exp(-rate * timeToMaturity);
		}
	}
@
;factor
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
	MULPD XMM10, XMM9				;XMM10 = [((((b5*t+b4)*t+b3)*t+b2)*t+b1)*t = factor]
;prepare exp arguments vector
	VORPD YMM11, YMM8, YMM2			;YMM11 = [-rate]
	VMULPD YMM11, YMM11, YMM4		;YMM11 = [-rate*timeToMaturity]
	VMULPD XMM12, XMM7, XMM7		;XMM12 = (|d1|^2,|d2|^2)
	MULPD XMM12, M05				;XMM12 = (-0.5*|d1|^2,-0.5*|d2|^2)
	MOVAPD XMM11, XMM12				;XMM11 = (-0.5*|d1|^2,-0.5*|d2|^2)

;YMM11 input = (-rate*timeToMaturity, -rate*timeToMaturity, -0.5*d1^2, -0.5*d2^2)
;Remez 4*parallel fast exp (max. rel. error = 1.72863156e-3 on [-87.33654, 88.72283])
;e^x = 2^(log2(e)*x)=2^(i+f) with -1<f<1
	VMULPD YMM11, YMM11, L2E		;YMM11 = [log2(e) * x = t]	
	VROUNDPD YMM4, YMM11, 1			;YMM4 = [floor(t)]
comment @ convert double to int64
	translate C {
		//  Only works for inputs in the range: [-2^51, 2^51]
		__m128i double_to_int64(__m128d x){
			x = _mm_add_pd(x, _mm_set1_pd(0x0018000000000000));
			return _mm_sub_epi64(
				_mm_castpd_si128(x),
				_mm_castpd_si128(_mm_set1_pd(0x0018000000000000))
			);
		}
	}
@
	VMOVUPD YMM12, CNVI
	VADDPD YMM13, YMM12, YMM4
	VPSUBQ YMM12, YMM13, YMM12		;YMM12 = [i = (int)floor(t)]
	VSUBPD YMM13, YMM11, YMM4		;YMM13 = [f = t - floor(t)]
;2^f polinomial fast approximation
	VMOVUPD YMM4, expC0				;YMM4 = [c0]
	VFMADD213PD YMM4, YMM13, expC1	;YMM4 = [c0*f+c1]
	VFMADD213PD YMM4, YMM13, expC2	;YMM4 = [(c0*f+c1)*f+c2 ~ 2^f]
	VPSLLQ YMM12, YMM12, 52			;position exponent in double IEEE representation (=> * 2^i)
	VPADDQ YMM11, YMM12, YMM4		;blend in 2^f => 2^f
;YMM11 output = (exp(-rate*timeToMaturity),exp(-rate*timeToMaturity),exp(-0.5*d1^2),exp(-0.5*d2^2))

comment @ complete post exp calculation (nd1, nd2, KexpMinusRT)
	translate C {
		double nd1 = normalCDF(d1), nd2 = normalCDF(d2);
		double MinusKexpMinusRT = -KexpMinusRT(strike, rate, timeToMaturity);
		double callValue = principal * nd1 + _KexpMinusRT * nd2; //notice dot product
	}
@
	XORPD XMM12, XMM12										;reset XMM12
	SUBPD XMM12, XMM1										;XMM12 = [-strike]
	MOVSD STRIKE, XMM12										;store -strike
	VMULPD YMM11, YMM11, YMMWORD PTR [one_div_sqrt_of_2pi]	;YMM11 = (-strike*exp(-rate*timeToMaturity)=-KexpMinusRT,0,sigma(|d1|), sigma(|d2|))
	MULPD XMM11, XMM10										;XMM11 = (factor*sigma(|d1|), factor*sigma(|d2|))
	VSUBPD XMM12, XMM15, XMM11								;XMM12 = (1-factor*sigma(|d1|), 1-factor*sigma(|d2|))
;selection of sigma*factor or (1 - sigma*factor) based on d12 sign
	VBLENDVPD XMM13, XMM12, XMM11, XMM6						;XMM13 = (nd1, nd2)
;prepare for dot product
	VPERM2F128 YMM14, YMM11, YMM11, 1						;YMM14 = (_, _, -KexpMinusRT, 0)
	ADDPD XMM14, XMM0										;XMM14 = (-KexpMinusRT, principal)
	SHUFPD XMM14, XMM14, 1									;XMM14 = (principal, -KexpMinusRT)
	VDPPD XMM0, XMM13, XMM14, 031h							;XMM0 = principal * nd1 - _KexpMinusRT * nd2 = RESULT
	RET
CallPrice ENDP
END