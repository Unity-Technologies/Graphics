#ifndef _PATHTRACING_PATHTRACINGMATERIALS_HLSL_
#define _PATHTRACING_PATHTRACINGMATERIALS_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/MaterialPool/MaterialPool.hlsl"

#define PTMaterial MaterialPool::MaterialEntry
#define MaterialProperties MaterialPool::MaterialProperties

StructuredBuffer<PTMaterial>    g_MaterialList;

// Textures
Texture2DArray<float4>          g_AlbedoTextures;
Texture2DArray<float4>          g_TransmissionTextures;
Texture2DArray<float4>          g_EmissionTextures;
float                           g_AtlasTexelSize; // The size of 1 texel in the atlases above

// Samplers
SamplerState                    sampler_g_EmissionTextures;
SamplerState                    sampler_g_AlbedoTextures;
SamplerState                    sampler_g_TransmissionTextures;

float                           g_AlbedoBoost;

float4 SampleAtlas(Texture2DArray<float4> atlas, SamplerState atlasSampler, uint index, float2 uv, float2 scale, float2 offset, bool pointFilterMode)
{
    return MaterialPool::SampleAtlas(atlas, atlasSampler, g_AtlasTexelSize, index, uv, scale, offset, pointFilterMode);
}

MaterialProperties LoadMaterialProperties(UnifiedRT::InstanceData instanceInfo, bool useWhiteAsBaseColor, float2 uv0, float2 uv1)
{
    MaterialProperties output = MaterialPool::LoadMaterialProperties(
        g_MaterialList,
        g_AlbedoTextures,
        sampler_g_AlbedoTextures,
        g_TransmissionTextures,
        sampler_g_TransmissionTextures,
        g_EmissionTextures,
        sampler_g_EmissionTextures,
        g_AlbedoBoost,
        g_AtlasTexelSize,
        instanceInfo.userMaterialID,
        uv0,
        uv1);

    if (useWhiteAsBaseColor)
    {
        // override to white albedo, because albedo is multiplied with lightmap color at runtime
        output.baseColor = float3(1, 1, 1);
    }

    return output;
}

#endif
