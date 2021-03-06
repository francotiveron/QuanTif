#include "stdafx.h"
#include <cmath>

#include "QuanTif.Cpp.CLI.h"

extern double normalCDF(double x);

namespace QuanTifCppCLI {
	double BlackScholesCppCLI::CallPrice(double principal, double strike, double rate, double volatility, double timeToMaturity) {
		double logSdivK = log(principal / strike);
		double sigmaSqrHalved = volatility * volatility * 0.5;
		double sigmaSqrT = volatility * pow(timeToMaturity, 0.5);
		double d1 = (logSdivK + (rate + sigmaSqrHalved) * timeToMaturity) / sigmaSqrT;
		double d2 = (logSdivK + (rate - sigmaSqrHalved) * timeToMaturity) / sigmaSqrT;
		double nd1 = normalCDF(d1);
		double nd2 = normalCDF(d2);
		double KexpMinusRT = strike * exp(-rate * timeToMaturity);
		return principal * nd1 - KexpMinusRT * nd2;
	}

	double BlackScholesCppCLI::CallPriceStress(double principal, double strike, double rate, double volatility, double timeToMaturity, int n) {
		for (int i = 1; i < n; i++) CallPrice(principal, strike, rate, volatility, timeToMaturity);
		return CallPrice(principal, strike, rate, volatility, timeToMaturity);
	}
}