#ifndef _PATHTRACING_PATHTRACINGMATERIALS_HLSL_
#define _PATHTRACING_PATHTRACINGMATERIALS_HLSL_

struct PTMaterial
{
    int     albedoTextureIndex;
    int     emissionTextureIndex;
    int     transmissionTextureIndex;
    uint    flags;
    float2  albedoScale;
    float2  albedoOffset;
    float2  emissionScale;
    float2  emissionOffset;
    float2  transmissionScale;
    float2  transmissionOffset;
    float3  emissionColor;
    uint    albedoAndEmissionUVChannel;
};

struct MaterialProperties
{
    float3 baseColor;
    float metalness;

    float3 emissive;
    float roughness;

    float3 transmission;
    uint isTransmissive;

    uint doubleSidedGI;
};

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
    // Apply the scale and offset to access to desired atlas entry
    float2 localUV = uv * scale + offset;

    // To prevent sampling part of the neighbor due to bilinear filtering, we need to clamp the sample
    // position to an 'inner rectangle' of the atlas entry, which is shrunk by half a texel on each side.
    float2 innerRectMin = offset + (g_AtlasTexelSize * 0.5f);
    // Calculating the rectangle extent is a bit tricky, because the size of the entry (scale)
    // is not necessarily a multiple of the atlas texel size. We need to round it up to the next texel first,
    // then subtract the half texel.
    float2 innerRectMax = ceil((offset + scale) / g_AtlasTexelSize) * g_AtlasTexelSize - (g_AtlasTexelSize * 0.5f);
    float2 clampedUV = clamp(localUV, innerRectMin, innerRectMax);

    [branch] if (pointFilterMode)
        return atlas.Load(int4(clampedUV * rcp(g_AtlasTexelSize), index, 0));
    else
        return atlas.SampleLevel(atlasSampler, float3(clampedUV, index), 0);
}

MaterialProperties LoadMaterialProperties(UnifiedRT::InstanceData instanceInfo, bool useWhiteAsBaseColor, PTHitGeom hitGeom)
{
    int materialIndex = instanceInfo.userMaterialID;
    PTMaterial matInfo = g_MaterialList[materialIndex];

    MaterialProperties material = (MaterialProperties)0;

    material.baseColor = float3(0.75, 0.75, 0.75);
    material.emissive = float3(0.0, 0.0, 0.0);
    material.transmission = float3(1.0, 1.0, 1.0);
    material.isTransmissive = matInfo.flags & 1;

    if (matInfo.albedoTextureIndex != -1)
    {
        float2 textureUV = matInfo.albedoAndEmissionUVChannel == 1 ? hitGeom.uv1 : hitGeom.uv0;
        float4 texColor = SampleAtlas(g_AlbedoTextures, sampler_g_AlbedoTextures, matInfo.albedoTextureIndex, textureUV, matInfo.albedoScale, matInfo.albedoOffset, false);
        material.baseColor = texColor.rgb;
        // apply albedo boost, but still keep the reflectance at maximum 100%
        material.baseColor = min(g_AlbedoBoost * material.baseColor, float3(1.0, 1.0, 1.0));
        material.transmission = 1.0f - float3(texColor.a, texColor.a, texColor.a);
    }

    if (matInfo.emissionTextureIndex != -1)
    {
        float2 textureUV = matInfo.albedoAndEmissionUVChannel == 1 ? hitGeom.uv1 : hitGeom.uv0;
        material.emissive = SampleAtlas(g_EmissionTextures, sampler_g_EmissionTextures, matInfo.emissionTextureIndex, textureUV, matInfo.emissionScale, matInfo.emissionOffset, false).rgb;
    }
    else
    {
        material.emissive = matInfo.emissionColor;
    }

    if (matInfo.transmissionTextureIndex != -1)
    {
        float2 uv = hitGeom.uv0;
        bool pointSampleTransmission = (matInfo.flags & 4) != 0;
        material.transmission = saturate(SampleAtlas(g_TransmissionTextures, sampler_g_TransmissionTextures, matInfo.transmissionTextureIndex, uv, matInfo.transmissionScale, matInfo.transmissionOffset, pointSampleTransmission).rgb);
    }

    // unused for now, we need these for specular support
    material.roughness = 1.0;
    material.metalness = 0;

    if (useWhiteAsBaseColor)
    {
        // override to white albedo, because albedo is multiplied with lightmap color at runtime
        material.baseColor = float3(1, 1, 1);
    }

    material.doubleSidedGI = matInfo.flags & 2;

    return material;
}

#endif
