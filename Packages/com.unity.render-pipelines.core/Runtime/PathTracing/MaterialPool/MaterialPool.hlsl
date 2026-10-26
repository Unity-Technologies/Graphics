#ifndef _MATERIAL_POOL_HLSL_
#define _MATERIAL_POOL_HLSL_

namespace MaterialPool
{
    struct MaterialEntry
    {
        int albedoTextureIndex;
        int emissionTextureIndex;
        int transmissionTextureIndex;
        uint flags;
        float2 albedoScale;
        float2 albedoOffset;
        float2 emissionScale;
        float2 emissionOffset;
        float2 transmissionScale;
        float2 transmissionOffset;
        float3 emissionColor;
        uint albedoAndEmissionUVChannel;
    };

    struct MaterialProperties
    {
        float3 baseColor;
        float metalness;

        float3 emission;
        float roughness;

        float3 transmission;
        uint isTransmissive;

        uint doubleSidedGI;
    };

    float4 SampleAtlas(Texture2DArray<float4> atlas, SamplerState atlasSampler, float atlasTexelSize, uint index, float2 uv, float2 scale, float2 offset, bool pointFilterMode)
    {
        // Apply the scale and offset to access to desired atlas entry
        float2 localUV = uv * scale + offset;

        // To prevent sampling part of the neighbor due to bilinear filtering, we need to clamp the sample
        // position to an 'inner rectangle' of the atlas entry, which is shrunk by half a texel on each side.
        float2 innerRectMin = offset + (atlasTexelSize * 0.5f);
        // Calculating the rectangle extent is a bit tricky, because the size of the entry (scale)
        // is not necessarily a multiple of the atlas texel size. We need to round it up to the next texel first,
        // then subtract the half texel.
        float2 innerRectMax = ceil((offset + scale) / atlasTexelSize) * atlasTexelSize - (atlasTexelSize * 0.5f);
        float2 clampedUV = clamp(localUV, innerRectMin, innerRectMax);

        [branch] if (pointFilterMode)
            return atlas.Load(int4(clampedUV * rcp(atlasTexelSize), index, 0));
        else
            return atlas.SampleLevel(atlasSampler, float3(clampedUV, index), 0);
    }

    float4 LoadAlbedo(
        MaterialEntry matEntry,
        Texture2DArray<float4> albedoTextures,
        SamplerState albedoSamplerState,
        float atlasTexelSize,
        float2 uv0,
        float2 uv1)
    {
        float4 albedo = float4(0.75, 0.75, 0.75, 0);

        if (matEntry.albedoTextureIndex != -1)
        {
            float2 textureUV = matEntry.albedoAndEmissionUVChannel == 1 ? uv1 : uv0;
            bool pointSampleAlbedo = (matEntry.flags & 8) != 0;
            albedo = SampleAtlas(albedoTextures, albedoSamplerState, atlasTexelSize, matEntry.albedoTextureIndex, textureUV, matEntry.albedoScale, matEntry.albedoOffset, pointSampleAlbedo);
        }

        return albedo;
    }

    float3 BoostAlbedo(float3 albedo, float albedoBoost)
    {
        // Apply albedo boost, but still keep the reflectance at maximum 100%
        return min(albedoBoost * albedo, float3(1.0, 1.0, 1.0));
    }

    float3 LoadAlbedoWithBoost(
        MaterialEntry matEntry,
        Texture2DArray<float4> albedoTextures,
        SamplerState albedoSamplerState,
        float atlasTexelSize,
        float albedoBoost,
        float2 uv0,
        float2 uv1)
    {
        float4 albedo = LoadAlbedo(matEntry, albedoTextures, albedoSamplerState, atlasTexelSize, uv0, uv1);
        return BoostAlbedo(albedo.rgb, albedoBoost);
    }

    float3 LoadEmission(
        MaterialEntry matEntry,
        Texture2DArray<float4> emissionTextures,
        SamplerState emissionSamplerState,
        float atlasTexelSize,
        float2 uv0,
        float2 uv1)
    {
        float3 emission;

        if (matEntry.emissionTextureIndex != -1)
        {
            float2 textureUV = matEntry.albedoAndEmissionUVChannel == 1 ? uv1 : uv0;
            bool pointSampleEmission = (matEntry.flags & 16) != 0;
            emission = SampleAtlas(emissionTextures, emissionSamplerState, atlasTexelSize, matEntry.emissionTextureIndex, textureUV, matEntry.emissionScale, matEntry.emissionOffset, pointSampleEmission).rgb;
        }
        else
        {
            emission = matEntry.emissionColor;
        }

        return emission;
    }

    float3 LoadTransmission(
        MaterialEntry matEntry,
        float4 albedo,
        Texture2DArray<float4> transmissionTextures,
        SamplerState transmissionSamplerState,
        float atlasTexelSize,
        float2 uv0)
    {
        float3 transmission = 1.0f - float3(albedo.a, albedo.a, albedo.a);

        if (matEntry.transmissionTextureIndex != -1)
        {
            bool pointSampleTransmission = (matEntry.flags & 4) != 0;
            transmission = saturate(SampleAtlas(transmissionTextures, transmissionSamplerState, atlasTexelSize, matEntry.transmissionTextureIndex, uv0, matEntry.transmissionScale, matEntry.transmissionOffset, pointSampleTransmission).rgb);
        }

        return transmission;
    }

    bool UsesDoubleSidedGI(uint materialFlags)
    {
        return materialFlags & 2;
    }

    MaterialProperties LoadMaterialProperties(
        StructuredBuffer<MaterialEntry> materialList,
        Texture2DArray<float4> albedoTextures,
        SamplerState albedoSamplerState,
        Texture2DArray<float4> transmissionTextures,
        SamplerState transmissionSamplerState,
        Texture2DArray<float4> emissionTextures,
        SamplerState emissionSamplerState,
        float albedoBoost,
        float atlasTexelSize,
        uint materialIndex,
        float2 uv0,
        float2 uv1)
    {
        const MaterialEntry matEntry = materialList[materialIndex];

        MaterialProperties material;

        float4 albedo = LoadAlbedo(matEntry, albedoTextures, albedoSamplerState, atlasTexelSize, uv0, uv1);

        material.baseColor = BoostAlbedo(albedo.rgb, albedoBoost);
        material.transmission = LoadTransmission(matEntry, albedo, transmissionTextures, transmissionSamplerState, atlasTexelSize, uv0);
        material.emission = LoadEmission(matEntry, emissionTextures, emissionSamplerState, atlasTexelSize, uv0, uv1);
        material.isTransmissive = matEntry.flags & 1;
        material.doubleSidedGI = UsesDoubleSidedGI(matEntry.flags);

        // unused for now, we need these for specular support
        material.roughness = 1.0;
        material.metalness = 0;

        return material;
    }
}

#endif
