// We need a noise texture for sampling
Texture2DArray<float4>						_RaytracingNoiseTexture;	
uint 										_RaytracingNoiseResolution;
uint 										_RaytracingNumNoiseLayers;

// Function that wraps the sampling of raytracing screenspace noise
float2 GetRaytracingNoiseSample(uint2 positionSS, int index)
{
	// We have only a limited number of layers for sampling
	uint texSlot = index % _RaytracingNumNoiseLayers;
	uint shiftW =  (index / _RaytracingNumNoiseLayers * 13);
	uint shiftH = (index / _RaytracingNumNoiseLayers * 29);
	return LOAD_TEXTURE2D_ARRAY(_RaytracingNoiseTexture, (positionSS  + uint2(shiftW , shiftH)) % _RaytracingNoiseResolution, texSlot).xy;
}

// Function to sample according the ggx function
float3 SampleGGXDir(float2 u, float roughness)
{
    float cosTheta = sqrt(SafeDiv(1.0 - u.x, 1.0 + (roughness * roughness - 1.0) * u.x));
    float phi      = TWO_PI * u.y;

    return SphericalToCartesian(phi, cosTheta);
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
