#include "stdafx.h"

#define _USE_MATH_DEFINES
#define b0 0.2316419
#define b1 0.319381530
#define b2 -0.356563782
#define b3 1.781477937
#define b4 -1.821255978
#define b5 1.330274429
#define one_div_sqrt_of_2pi 0.3989422804

#include <iostream>
#include <cmath>
#include <tuple>

using namespace std;

double normalPDF(double x) {
	return one_div_sqrt_of_2pi * exp(-0.5 * x * x);
}

double normalCDF(double x) {
	if (x >= 0.0) {
		double t = 1. / (1. + b0 * x);
		double sigma = normalPDF(x);
		double factor = t * (b1 + t * (b2 + t * (b3 + t * (b4 + t * b5))));
		return 1.0 - sigma * factor;
	}
	else {
		return 1.0 - normalCDF(-x);
	}
}
tuple<double, double> d12(double principal, double strike, double rate, double volatility, double timeToMaturity) {
	double logSdivK = log(principal / strike);
	double sigmaSqrHalved = volatility * volatility * 0.5;
	double sigmaSqrT = volatility * pow(timeToMaturity, 0.5);
	return tuple<double, double>((logSdivK + (rate + sigmaSqrHalved) * timeToMaturity) / sigmaSqrT, (logSdivK + (rate - sigmaSqrHalved) * timeToMaturity) / sigmaSqrT);
}

double KexpMinusRT(double strike, double rate, double timeToMaturity) {
	return strike * exp(-rate * timeToMaturity);
}

tuple<double, double> calcOptions(double principal, double strike, double rate, double volatility, double timeToMaturity, bool withPut) {
	tuple<double, double> _d12 = d12(principal, strike, rate, volatility, timeToMaturity);
	double nd1 = normalCDF(get<0>(_d12)), nd2 = normalCDF(get<1>(_d12));
	double _KexpMinusRT = KexpMinusRT(strike, rate, timeToMaturity);
	double callValue = principal * nd1 - _KexpMinusRT * nd2;
	if (withPut) 
		return tuple<double, double>(callValue, _KexpMinusRT - principal + callValue); 
	else 
		return tuple<double, double>(callValue, 0.);
}

double callPrice(double principal, double strike, double rate, double volatility, double timeToMaturity) {
	return get<0>(calcOptions(principal, strike, rate, volatility, timeToMaturity, false));
}

double putPrice(double principal, double strike, double rate, double volatility, double timeToMaturity) {
	return get<1>(calcOptions(principal, strike, rate, volatility, timeToMaturity, true));
}

tuple<double, double> optionsPrice(double principal, double strike, double rate, double volatility, double timeToMaturity) {
	return calcOptions(principal, strike, rate, volatility, timeToMaturity, true);
}
