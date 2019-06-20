// We need a noise texture for sampling
Texture2DArray<float4>						_RaytracingNoiseTexture;	
uint 										_RaytracingNoiseResolution;
uint 										_RaytracingNumNoiseLayers;

float GetRadicalInverse(uint n, uint scramble)
{
    n = (n << 16) | (n >> 16);
    n = ((n & 0x00ff00ff) << 8) | ((n & 0xff00ff00) >> 8);
    n = ((n & 0x0f0f0f0f) << 4) | ((n & 0xf0f0f0f0) >> 4);
    n = ((n & 0x33333333) << 2) | ((n & 0xcccccccc) >> 2);
    n = ((n & 0x55555555) << 1) | ((n & 0xaaaaaaaa) >> 1);

    // Account for the available precision and scramble
    n = (n >> (32 - 24)) ^ (scramble & ~-(1 << 24));

    return float(n) / float(1 << 24);
}

float GetSobol(uint n, uint scramble)
{
    for (uint v = 1 << 31; n != 0; n >>= 1, v ^= v >> 1)
        if (n & 1)
            scramble ^= v;
    return float(scramble) / 4294967296.0f; // 2^32
}

float2 Sample02(uint n, uint2 scramble)
{
    return float2(GetRadicalInverse(n, scramble.x), GetSobol(n, scramble.y));
}

// Function that wraps the sampling of raytracing screenspace noise
float2 GetRaytracingNoiseSample(uint2 positionSS, int index)
{
	// We have only a limited number of layers for sampling
	uint texSlot = index % _RaytracingNumNoiseLayers;
	uint shiftW =  (index / _RaytracingNumNoiseLayers * 13);
	uint shiftH = (index / _RaytracingNumNoiseLayers * 29);
	return LOAD_TEXTURE2D_ARRAY(_RaytracingNoiseTexture, (positionSS  + uint2(shiftW , shiftH)) % _RaytracingNoiseResolution, texSlot).xy;
}

// TODO: Use the tangent to create the local orthobasis
void CreatePixarOrthoNormalBasis(float3 n, out float3 tangent, out float3 bitangent)
{
	float sign = n.z > 0.0f ? 1.0f : -1.0f;
    float a = -1.0 / (sign + n.z);
    float b = n.x * n.y * a;
    tangent = float3(1.0 + sign * n.x * n.x * a, sign * b, -sign * n.x);
    bitangent = float3(b, sign + n.y * n.y * a, -n.y);
}
