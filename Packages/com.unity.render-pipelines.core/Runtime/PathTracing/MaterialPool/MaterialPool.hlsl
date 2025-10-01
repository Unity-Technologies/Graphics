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

        float3 emissive;
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

        MaterialProperties material = (MaterialProperties)0;
        material.baseColor = float3(0.75, 0.75, 0.75);
        material.transmission = float3(1.0, 1.0, 1.0);
        material.isTransmissive = matEntry.flags & 1;

        if (matEntry.albedoTextureIndex != -1)
        {
            float2 textureUV = matEntry.albedoAndEmissionUVChannel == 1 ? uv1 : uv0;
            float4 texColor = SampleAtlas(albedoTextures, albedoSamplerState, atlasTexelSize, matEntry.albedoTextureIndex, textureUV, matEntry.albedoScale, matEntry.albedoOffset, false);
            material.baseColor = texColor.rgb;
            // apply albedo boost, but still keep the reflectance at maximum 100%
            material.baseColor = min(albedoBoost * material.baseColor, float3(1.0, 1.0, 1.0));
            material.transmission = 1.0f - float3(texColor.a, texColor.a, texColor.a);
        }

        if (matEntry.emissionTextureIndex != -1)
        {
            float2 textureUV = matEntry.albedoAndEmissionUVChannel == 1 ? uv1 : uv0;
            material.emissive = SampleAtlas(emissionTextures, emissionSamplerState, atlasTexelSize, matEntry.emissionTextureIndex, textureUV, matEntry.emissionScale, matEntry.emissionOffset, false).rgb;
        }
        else
        {
            material.emissive = matEntry.emissionColor;
        }

        if (matEntry.transmissionTextureIndex != -1)
        {
            float2 uv = uv0;
            bool pointSampleTransmission = (matEntry.flags & 4) != 0;
            material.transmission = saturate(SampleAtlas(transmissionTextures, transmissionSamplerState, atlasTexelSize, matEntry.transmissionTextureIndex, uv, matEntry.transmissionScale, matEntry.transmissionOffset, pointSampleTransmission).rgb);
        }

        // unused for now, we need these for specular support
        material.roughness = 1.0;
        material.metalness = 0;
        material.doubleSidedGI = matEntry.flags & 2;

        return material;
    }
}

#endif
