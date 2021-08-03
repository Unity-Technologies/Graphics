#ifndef RAY_TRACING_COMMON_HLSL
#define RAY_TRACING_COMMON_HLSL
// This array converts an index to the local coordinate shift of the half resolution texture
static const uint2 HalfResIndexToCoordinateShift[4] = { uint2(0,0), uint2(1, 0), uint2(0, 1), uint2(1, 1) };

// Heuristic mapping from roughness (GGX in particular) to ray spread angle
float roughnessToSpreadAngle(float roughness)
{
    // FIXME: The mapping will most likely need adjustment...
    return roughness * PI/8;
}

// #define USE_RAY_CONE_LOD

float computeBaseTextureLOD(float3 viewWS,
                            float3 normalWS,
                            float  coneWidth,
                            float  areaUV,
                            float  areaWS)
{
    // Compute LOD following the ray cone formulation in Ray Tracing Gems (20.3.4)
    float lambda = 0.5 * log2(areaUV / areaWS);
    lambda += log2(abs(coneWidth / dot(viewWS, normalWS)));

    return lambda;
}

float computeTargetTextureLOD(Texture2D targetTexture, float baseLambda)
{
    // Grab dimensions of the target texture
    uint texWidth, texHeight;
    targetTexture.GetDimensions(texWidth, texHeight);

    return max(0.0, baseLambda + 0.5 * log2(texWidth * texHeight));
}

// The standard lit data used for paking intersection data for deferred lighting (for ray tracing)
struct StandardBSDFData
{
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float3 fresnel0;
    float coatMask;
    float3 emissiveAndBaked;
    uint renderingLayers;
    float4 shadowMasks;
    uint isUnlit;
};

// This function defines what is the source pixel from where we should read the depth and normal for rendering in half resolution
uint2 ComputeSourceCoordinates(uint2 halfResCoord, int frameIndex)
{
    return halfResCoord * 2;
}

#endif // RAY_TRACING_COMMON_HLSL