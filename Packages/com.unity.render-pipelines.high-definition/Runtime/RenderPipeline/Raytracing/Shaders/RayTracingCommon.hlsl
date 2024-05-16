#ifndef RAY_TRACING_COMMON_HLSL
#define RAY_TRACING_COMMON_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"

#if SHADER_API_GAMECORE_XBOXSERIES
// On GameCore we currently don't support recursive raytracing modes, so stripping TraceRay from hit group shaders will enforces this and allow better shader optimization
// SHADER_STAGE_RAYTRACING is defined only for shaders imported from raytrace files ( note: SHADER_STAGE_RAY_TRACING is defined for all raytracing shaders including hitgroups )
#if !SHADER_STAGE_RAYTRACING
#define TraceRay(d2p_accelStruct,d2p_rayFlags,d2p_instanceMask,d2p_contribution,d2p_multiplier,d2p_missShader,d2p_ray,d2p_payload) ;//TraceRay()_removed_in_function_shader
#endif
#endif

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

uint2 ComputeCheckerBoardOffset(uint2 traceCoord, uint subPixelIndex)
{
    uint checker = (traceCoord.x & 1) ^ (traceCoord.y & 1);
    subPixelIndex = (subPixelIndex + checker) & 0x3;
    return uint2(((subPixelIndex >> 1) ^ subPixelIndex) & 1, subPixelIndex >> 1);
}

// This function compute the checkerboard undersampling position
// Warning: This function is broken, but keeping it to not break anything
// Use ComputeCheckerBoardOffset instead
uint ComputeCheckerBoardIndex(uint2 traceCoord, uint subPixelIndex)
{
    // TODO: missing parenthesis around the & operations
    uint localOffset = (traceCoord.x & 1 + traceCoord.y & 1) & 1;
    uint checkerBoardLocation = (subPixelIndex + localOffset) & 0x3;
    return checkerBoardLocation;
}

uint2 HalfResolutionIndexToOffset(uint index)
{
    return uint2(index & 0x1, index / 2);
}

// This function defines what is the source pixel from where we should read the depth and normal for rendering in half resolution
uint2 ComputeSourceCoordinates(uint2 halfResCoord, uint subPixelIndex)
{
    // TODO, there remains spots where the half res checker board pattern is still broken, there needs to be a pass done on it
    uint checkerBoardIndex = ComputeCheckerBoardIndex(halfResCoord, subPixelIndex);
    return halfResCoord * 2; //+ HalfResolutionIndexToOffset(checkerBoardIndex);
}

// These need to be negative for RayDistanceIndicatesHitSkyOrUnlit
#define RAY_TRACING_DISTANCE_FLAG_SKY 0.0

bool RayTracingGBufferIsUnlit(float rayDistance)
{
    return rayDistance < 0.0;
}

bool RayTracingGBufferIsSky(float rayDistance)
{
    return rayDistance == RAY_TRACING_DISTANCE_FLAG_SKY;
}

bool RayTracingGBufferIsLit(float rayDistance)
{
    return rayDistance > 0.0;
}

float3 RayTracingHSVClamp(float3 color, float clampValue)
{
    // Convert to HSV space
    float3 hsvColor = RgbToHsv(color);

    // Expose and clamp the final color
    hsvColor.z = clamp(hsvColor.z, 0.0, clampValue);

    // Convert back to HSV space
    return HsvToRgb(hsvColor);
}

#endif // RAY_TRACING_COMMON_HLSL
