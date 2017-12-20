// Library header containing various useful functions for doing moment based shadow maps.
// Supported flavors are VSM, EVSM and MSM


// conversion helper for VSM flavors
// Chebychev's inequality (one-tailed version)
// P( x >= t ) <= pmax(t) := sigma^2 / (sigma^2 + (t - u)^2)
// for us t is depth, u is E(x) i.d. the blurred depth
REAL ShadowMoments_ChebyshevsInequality( REAL2 moments, REAL depth, REAL minVariance, REAL lightLeakBias )
{
	// variance sig^2 = E(x^2) - E(x)^2
	REAL variance = max( moments.y - (moments.x * moments.x), minVariance );

	// probabilistic upper bound
	REAL mD = depth - moments.x;
	REAL p = variance / (variance + mD * mD);

	p = saturate( (p - lightLeakBias) / (1.0 - lightLeakBias) );
	return max( p, depth <= moments.x );
}

// helper for EVSM
REAL2 ShadowMoments_WarpDepth( REAL depth, REAL2 exponents )
{
	// Rescale depth into [-1;1]
	depth = 2.0 * depth - 1.0;
	REAL pos =  exp( exponents.x * depth );
	REAL neg = -exp(-exponents.y * depth );
	return REAL2( pos, neg );
}

// helpers for MSM
// Prepare the moments so there's little quantization error when storing the moments at REAL
// precision. This step becomes unnecessary if the moments are stored in 32bit floats.
REAL4 ShadowMoments_Encode16MSM( REAL depth )
{
	REAL dsq = depth * depth;
	REAL4 moments = { depth, dsq, depth * dsq, dsq * dsq };
	REAL4x4 mat = { - 2.07224649  ,  13.7948857237,  0.105877704 ,   9.7924062118,
					  32.23703778  , -59.4683975703, -1.9077466311, -33.7652110555,
					 -68.571074599 ,  82.0359750338,  9.3496555107,  47.9456096605,
					  39.3703274134, -35.364903257 , -6.6543490743, -23.9728048165 };

	REAL4 optimized     = mul( moments, mat );
		   optimized[0] += 0.035955884801;

	return optimized;
}

REAL4 ShadowMoments_Decode16MSM( REAL4 moments )
{
	moments[0] -= 0.035955884801;
	REAL4x4 mat = { 0.2227744146,  0.1549679261,  0.1451988946,  0.163127443,
					 0.0771972861,  0.1394629426,  0.2120202157,  0.2591432266,
					 0.7926986636,  0.7963415838,  0.7258694464,  0.6539092497,
					 0.0319417555, -0.1722823173, -0.2758014811, -0.3376131734 };
	return mul( moments, mat );
}

// Note: Don't call this with all moments being equal or 0.0, otherwise this code degenerates into lots of +/-inf calculations
//       which don't behave quite the same on all hardware.
void ShadowMoments_SolveMSM( REAL4 moments, REAL depth, REAL momentBias, out REAL3 z, out REAL4 b )
{
	// Bias input data to avoid artifacts
	z[0] = depth;
	b    = lerp( moments, 0.5.xxxx, momentBias );

	// Compute a Cholesky factorization of the Hankel matrix B storing only non-trivial entries or related products
	REAL L32D22     = mad( -b[0], b[1], b[2] );
	REAL D22        = mad( -b[0], b[0], b[1] );
	REAL sqDepthVar = mad( -b[1], b[1], b[3] );
	REAL D33D22     = dot( REAL2( sqDepthVar, -L32D22 ), REAL2( D22, L32D22 ) );
	REAL InvD22     = 1.0 / D22;
	REAL L32        = L32D22 * InvD22;
	// Obtain a scaled inverse image of bz = ( 1, z[0], z[0]*z[0] )^T
	REAL3 c = REAL3( 1.0, z[0], z[0] * z[0] );
	// Forward substitution to solve L * c1 = bz;
	c[1] -= b.x;
	c[2] -= b.y + L32 * c[1];
	// Scaling to solve D * c2 = c1
	c[1] *= InvD22;
	c[2] *= D22 / D33D22;
	// Backward substitution to solve L^T * c3 = c2
	c[1] -= L32 * c[2];
	c[0] -= dot( c.yz, b.xy );
	// Solve the quadratic equation c[0] + c[1] * z + c[2] * z^2 to obtain solutions z[1] and z[2]
	REAL p = c[1] / c[2];
	REAL q = c[0] / c[2];
	REAL D = ((p*p) * 0.25) - q;
	REAL r = sqrt( D );
	z[1] = -(p * 0.5) - r;
	z[2] = -(p * 0.5) + r;
}

REAL ShadowMoments_SolveDelta3MSM( REAL3 z, REAL2 b, REAL lightLeakBias )
{
	// Construct a solution composed of three Dirac-deltas and evaluate its CDF
	REAL4 switchVal = (z[2] < z[0]) ? REAL4( z[1], z[0], 1.0, 1.0 )
					: ((z[1] < z[0]) ? REAL4( z[0], z[1], 0.0, 1.0 ) : 0.0.xxxx);

	REAL quotient = (switchVal[0] * z[2] - b[0] * (switchVal[0] + z[2]) + b[1]) / ((z[2] - switchVal[1]) * (z[0] - z[1]));
	REAL attenuation = saturate( switchVal[2] + switchVal[3] * quotient );

	return saturate( ((1.0 - attenuation) - lightLeakBias) / (1.0 - lightLeakBias) );
}

REAL ShadowMoments_SolveDelta4MSM( REAL3 z, REAL4 b, REAL lightLeakBias)
{
	// Use a solution made of four deltas
	REAL zFree = ((b[2] - b[1]) * z[0] + b[2] - b[3]) / ((b[1] - b[0]) * z[0] + b[1] - b[2]);
	REAL w1Factor = (z[0] > zFree) ? 1.0 : 0.0;
	REAL attenuation = saturate( (b[1] - b[0] + (b[2] - b[0] - (zFree + 1.0) * (b[1] - b[0])) * (zFree - w1Factor - z[0]) / (z[0] * (z[0] - zFree))) / (zFree - w1Factor) + 1.0 - b[0] );

	return saturate( ((1.0 - attenuation) - lightLeakBias) / (1.0 - lightLeakBias) );
}
