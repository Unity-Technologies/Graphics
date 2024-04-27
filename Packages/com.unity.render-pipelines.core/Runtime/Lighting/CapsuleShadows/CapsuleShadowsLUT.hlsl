#ifndef CAPSULE_SHADOWS_LUT_HLSL
#define CAPSULE_SHADOWS_LUT_HLSL

#define CAPSULE_SHADOWS_LUT_MAX_RADIUS      2.f

uint XorShift32(uint x)
{
	// ref: https://en.wikipedia.org/wiki/Xorshift
	x ^= x << 13;
	x ^= x >> 17;
	x ^= x << 5;
	return x;
}

float UnitFloatFromHighBits(uint x)
{
	// put the high bits into the mantissa of 1.0 (for 1.0-2.0), then subtract 1.0
	return asfloat(0x3f800000U | (x >> 9)) - 1.f;
}

float2 SampleDiscUniform(float2 u)
{
	// ref: http://psgraphics.blogspot.com/2011/01/improved-code-for-concentric-map.html
	float a = 2.f*u.x - 1.f;
	float b = 2.f*u.y - 1.f;

	float r, phi;
	if (abs(a) > abs(b))
	{
		r = a;
		phi = .25f*PI*b/a;
	}
	else
	{
		r = b;
		phi = .5f*PI - .25f*PI*a/b;
	}

	float s, c;
	sincos(phi, s, c);
	return r*float2(c, s);
}

float ComputeCapsuleShadowLUT(
    float2 q,
    float r,
    inout uint rngState)
{
	int gridSize = 64;
	float gridSizeRcp = 1.f/(float)gridSize;
	uint capsuleCount = 0;
	for (int y = 0; y < gridSize; ++y)
	{
		for (int x = 0; x < gridSize; ++x)
		{
			// stratified jittered sampling of unit square
			float2 jitter = float2(
				UnitFloatFromHighBits(rngState & 0xffff0000),
				UnitFloatFromHighBits(rngState << 16));
			float2 u = (float2(x, y) + jitter)/(float)gridSize;

			// remap to unit disc
			float2 p = SampleDiscUniform(u);

			// check if we hit the capsule
			float2 pq = p - q;
			float2 pg = p - float2(q.x, 0.f);
			bool inRectangle = (abs(pq.x) <= r) && (0.f < p.y && p.y < q.y);
			bool inCircleA = (dot(pq, pq) < r*r);
			bool inCircleB = (dot(pg, pg) < r*r);
			if ((inRectangle || inCircleA) && !inCircleB)
				++capsuleCount;

			// advance RNG
			rngState = XorShift32(rngState);
		}
	}
    return (float)capsuleCount/(float)(gridSize*gridSize);
}

float ComputeCapsuleShadowLUTFromCoord(
    uint3 lutCoord,
    float3 lutCoordScale, // 1/(sizes - 1)
    inout uint rngState)
{
	float3 lutT = float3(lutCoord)*lutCoordScale;

	float r = lutT.z*CAPSULE_SHADOWS_LUT_MAX_RADIUS;
	float2 q = lutT.xy*(1.f + r);

    return ComputeCapsuleShadowLUT(q, r, rngState);
}

bool CapsuleCoordsHaveSameSign(float h1, float h2)
{
    return ((asuint(h1) ^ asuint(h2)) >> 31) == 0;
}

float ExtraCapsuleShadowReference(
    float d,
    float h1,
    float h2,
    float r)
{
    uint rngState = 0x1234abcd;
    float o1 = ComputeCapsuleShadowLUT(float2(d, abs(h1)), r, rngState);
    float o2 = ComputeCapsuleShadowLUT(float2(d, abs(h2)), r, rngState);
    return CapsuleCoordsHaveSameSign(h1, h2) ? abs(o1 - o2) : (o1 + o2);
}

float ExtraCapsuleShadowFromLUT(
    TEXTURE3D_PARAM(lutTexture, lutSampler),
    float3 lutCoordScale, // (sizes - 1)/sizes
    float3 lutCoordOffset, // 0.5/sizes
    float d,
    float h1,
    float h2,
    float r)
{
    r = min(r, CAPSULE_SHADOWS_LUT_MAX_RADIUS);

    float u = d/(1.f + r);
    float2 v = float2(abs(h1), abs(h2))/(1.f + r);
    float w = r/CAPSULE_SHADOWS_LUT_MAX_RADIUS;

    u = u*lutCoordScale.x + lutCoordOffset.x;
    v = v*lutCoordScale.y + lutCoordOffset.y;
    w = w*lutCoordScale.z + lutCoordOffset.z;

    float o1 = SAMPLE_TEXTURE3D_LOD(lutTexture, lutSampler, float3(u, v.x, w), 0).x;
    float o2 = SAMPLE_TEXTURE3D_LOD(lutTexture, lutSampler, float3(u, v.y, w), 0).x;

    return CapsuleCoordsHaveSameSign(h1, h2) ? abs(o1 - o2) : (o1 + o2);
}

#endif // ndef CAPSULE_SHADOWS_LUT_HLSL
