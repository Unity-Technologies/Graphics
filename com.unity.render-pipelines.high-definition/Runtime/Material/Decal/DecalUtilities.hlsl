#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"

#ifndef SCALARIZE_LIGHT_LOOP
#define SCALARIZE_LIGHT_LOOP (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && SHADERPASS == SHADERPASS_FORWARD)
#endif

DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

// Caution: We can't compute LOD inside a dynamic loop. The gradient are not accessible.
// we need to find a way to calculate mips. For now just fetch first mip of the decals
void ApplyBlendNormal(inout float4 dst, inout uint matMask, float2 texCoords, uint mapMask, float3x3 decalToWorld, float blend, float lod)
{
    float4 src;
    src.xyz = mul(decalToWorld, UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, texCoords, lod))) * 0.5 + 0.5;
    src.w = blend;
    dst.xyz = src.xyz * src.w + dst.xyz * (1.0 - src.w);
    dst.w = dst.w * (1.0 - src.w);
    matMask |= mapMask;
}

void ApplyBlendDiffuse(inout float4 dst, inout uint matMask, float2 texCoords, float4 src, uint mapMask, inout float blend, float lod, int diffuseTextureBound)
{
    if (diffuseTextureBound)
    {
        src *= SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, texCoords, lod);
    }
    src.w *= blend;
    blend = src.w;  // diffuse texture alpha affects all other channels
    dst.xyz = src.xyz * src.w + dst.xyz * (1.0 - src.w);
    dst.w = dst.w * (1.0 - src.w);
    matMask |= mapMask;
}



// albedoBlend is overall decal blend combined with distance fade and albedo alpha
// decalBlend is decal blend with distance fade to be able to construct normal and mask blend if they come from mask map blue channel
// normalBlend is calculated in this function and used later to blend the normal
// blendParams are material settings to determing blend source and mode for normal and mask map
void ApplyBlendMask(inout float4 dbuffer2, inout float2 dbuffer3, inout uint matMask, float2 texCoords, uint mapMask, float albedoBlend, float lod, float decalBlend, inout float normalBlend, float3 blendParams, float4 scalingMAB, float4 remappingAOS) // too many blends!!!
{
    float4 src = SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, texCoords, lod);
    src.x = scalingMAB.x * src.x;
    src.y = lerp(remappingAOS.x, remappingAOS.y, src.y);
    src.z = scalingMAB.z * src.z;
    src.w = lerp(remappingAOS.z, remappingAOS.w, src.w);
    float maskBlend;
    if (blendParams.x == 1.0)	// normal blend source is mask blue channel
        normalBlend = src.z * decalBlend;
    else
        normalBlend = albedoBlend; // normal blend source is albedo alpha

    if (blendParams.y == 1.0)	// mask blend source is mask blue channel
        maskBlend = src.z * decalBlend;
    else
        maskBlend = albedoBlend; // mask blend siurce is albedo alpha

    src.z = src.w;	// remap so smoothness goes to blue and mask blend goes to alpha
    src.w = maskBlend;

    float4 dbuffer2Mask;
    float2 dbuffer3Mask;

    if (blendParams.z == 0)
    {
        dbuffer2Mask = float4(1, 1, 1, 1);	// M, AO, S, S alpha
        dbuffer3Mask = float2(1, 1); // M alpha, AO alpha
    }
    else if (blendParams.z == 1)
    {
        dbuffer2Mask = float4(1, 0, 0, 0);	// M, _, _, _
        dbuffer3Mask = float2(1, 0); // M alpha, _
    }
    else if (blendParams.z == 2)
    {
        dbuffer2Mask = float4(0, 1, 0, 0);	// _, AO, _, _
        dbuffer3Mask = float2(0, 1); // _, AO alpha
    }
    else if (blendParams.z == 3)
    {
        dbuffer2Mask = float4(1, 1, 0, 0);	// M, AO, _, _
        dbuffer3Mask = float2(1, 1); // M Alpha, AO alpha
    }
    else if (blendParams.z == 4)
    {
        dbuffer2Mask = float4(0, 0, 1, 1);	// _, _, S, S alpha
        dbuffer3Mask = float2(0, 0); // _, _
    }
    else if (blendParams.z == 5)
    {
        dbuffer2Mask = float4(1, 0, 1, 1);	// M, _, S, S alpha
        dbuffer3Mask = float2(1, 0); // M alpha, _
    }
    else if (blendParams.z == 6)
    {
        dbuffer2Mask = float4(0, 1, 1, 1);	// _, AO, S, S alpha
        dbuffer3Mask = float2(0, 1); // _, AO alpha
    }
    else if (blendParams.z == 7)
    {
        dbuffer2Mask = float4(1, 1, 1, 1);	// M, AO, S, S alpha
        dbuffer3Mask = float2(1, 1); // M alpha, AO alpha
    }

    dbuffer2.xyz = (dbuffer2Mask.xyz == 1) ? src.xyz * src.w + dbuffer2.xyz * (1.0 - src.w) : dbuffer2.xyz;
    dbuffer2.w = (dbuffer2Mask.w == 1) ? dbuffer2.w * (1.0 - src.w) : dbuffer2.w;

    dbuffer3.xy = (dbuffer3Mask.xy == 1) ? dbuffer3.xy * (1.0 - src.w) : dbuffer3.xy;

    matMask |= mapMask;
}

// In order that the lod for with transpartent decal better match the lod for opaque decal
// We use ComputeTextureLOD with bias == 0.5
void EvalDecalMask(PositionInputs posInput, float3 positionRWSDdx, float3 positionRWSDdy, DecalData decalData,
    inout float4 DBuffer0, inout float4 DBuffer1, inout float4 DBuffer2, inout float2 DBuffer3, inout uint mask, inout float alpha)
{
    // Get the relative world camera to decal matrix
    float4x4 worldToDecal = ApplyCameraTranslationToInverseMatrix(decalData.worldToDecal);
    float3 positionDS = mul(worldToDecal, float4(posInput.positionWS, 1.0)).xyz;
    positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5, 0.5);  // decal clip space
    if ((all(positionDS.xyz > 0.0) && all(1.0 - positionDS.xyz > 0.0)))
    {
        float2 uvScale = float2(decalData.normalToWorld[3][0], decalData.normalToWorld[3][1]);
        float2 uvBias = float2(decalData.normalToWorld[3][2], decalData.normalToWorld[3][3]);
        positionDS.xz = positionDS.xz * uvScale + uvBias;
        positionDS.xz = frac(positionDS.xz);

        // clamp by half a texel to avoid sampling neighboring textures in the atlas
        float2 clampAmount = float2(0.5 / _DecalAtlasResolution.x, 0.5 / _DecalAtlasResolution.y);

        float2 diffuseMin = decalData.diffuseScaleBias.zw + clampAmount;                                    // offset into atlas is in .zw
        float2 diffuseMax = decalData.diffuseScaleBias.zw + decalData.diffuseScaleBias.xy - clampAmount;    // scale relative to full atlas size is in .xy so total texture extent in atlas is (1,1) * scale

        float2 normalMin = decalData.normalScaleBias.zw + clampAmount;
        float2 normalMax = decalData.normalScaleBias.zw + decalData.normalScaleBias.xy - clampAmount;

        float2 maskMin = decalData.maskScaleBias.zw + clampAmount;
        float2 maskMax = decalData.maskScaleBias.zw + decalData.maskScaleBias.xy - clampAmount;

        float2 sampleDiffuse = clamp(positionDS.xz * decalData.diffuseScaleBias.xy + decalData.diffuseScaleBias.zw, diffuseMin, diffuseMax);
        float2 sampleNormal = clamp(positionDS.xz * decalData.normalScaleBias.xy + decalData.normalScaleBias.zw, normalMin, normalMax);
        float2 sampleMask = clamp(positionDS.xz * decalData.maskScaleBias.xy + decalData.maskScaleBias.zw, maskMin, maskMax);

        // need to compute the mipmap LOD manually because we are sampling inside a loop
        float3 positionDSDdx = mul(worldToDecal, float4(positionRWSDdx, 0.0)).xyz; // transform the derivatives to decal space, any translation is irrelevant
        float3 positionDSDdy = mul(worldToDecal, float4(positionRWSDdy, 0.0)).xyz;

        float2 sampleDiffuseDdx = positionDSDdx.xz * decalData.diffuseScaleBias.xy; // factor in the atlas scale
        float2 sampleDiffuseDdy = positionDSDdy.xz * decalData.diffuseScaleBias.xy;
        float  lodDiffuse       = ComputeTextureLOD(sampleDiffuseDdx, sampleDiffuseDdy, _DecalAtlasResolution, 0.5);

        float2 sampleNormalDdx  = positionDSDdx.xz * decalData.normalScaleBias.xy;
        float2 sampleNormalDdy  = positionDSDdy.xz * decalData.normalScaleBias.xy;
        float  lodNormal        = ComputeTextureLOD(sampleNormalDdx, sampleNormalDdy, _DecalAtlasResolution, 0.5);

        float2 sampleMaskDdx    = positionDSDdx.xz * decalData.maskScaleBias.xy;
        float2 sampleMaskDdy    = positionDSDdy.xz * decalData.maskScaleBias.xy;
        float  lodMask          = ComputeTextureLOD(sampleMaskDdx, sampleMaskDdy, _DecalAtlasResolution, 0.5);

        float albedoBlend = decalData.normalToWorld[0][3];
        float4 src = decalData.baseColor;
        int diffuseTextureBound = (decalData.diffuseScaleBias.x > 0) && (decalData.diffuseScaleBias.y > 0);

        ApplyBlendDiffuse(DBuffer0, mask, sampleDiffuse, src, DBUFFERHTILEBIT_DIFFUSE, albedoBlend, lodDiffuse, diffuseTextureBound);
        alpha = alpha < albedoBlend ? albedoBlend : alpha;    // use decal alpha if it is higher than transparent alpha

        float albedoContribution = decalData.normalToWorld[1][3];
        if (albedoContribution == 0.0)
        {
            mask = 0;	// diffuse will not get modified
        }

        float normalBlend = albedoBlend;
        if ((decalData.maskScaleBias.x > 0) && (decalData.maskScaleBias.y > 0))
        {
            ApplyBlendMask(DBuffer2, DBuffer3, mask, sampleMask, DBUFFERHTILEBIT_MASK, albedoBlend, lodMask, decalData.normalToWorld[0][3], normalBlend, decalData.blendParams, decalData.scalingMAB, decalData.remappingAOS);
        }

        if ((decalData.normalScaleBias.x > 0) && (decalData.normalScaleBias.y > 0))
        {
            ApplyBlendNormal(DBuffer1, mask, sampleNormal, DBUFFERHTILEBIT_NORMAL, (float3x3)decalData.normalToWorld, normalBlend, lodNormal);
        }
    }
}

#if defined(_SURFACE_TYPE_TRANSPARENT) && defined(HAS_LIGHTLOOP) // forward transparent using clustered decals
DecalData FetchDecal(uint start, uint i)
{
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    int j = FetchIndex(start, i);
#else
    int j = start + i;
#endif
    return _DecalDatas[j];
}

DecalData FetchDecal(uint index)
{
    return _DecalDatas[index];
}
#endif

DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, inout float alpha)
{
    uint mask = 0;
    // the code in the macros, gets moved inside the conditionals by the compiler
    FETCH_DBUFFER(DBuffer, _DBufferTexture, int2(posInput.positionSS.xy));

#if defined(_SURFACE_TYPE_TRANSPARENT) && defined(HAS_LIGHTLOOP)  // forward transparent using clustered decals
    uint decalCount, decalStart;
    DBuffer0 = float4(0.0, 0.0, 0.0, 1.0);
    DBuffer1 = float4(0.5, 0.5, 0.5, 1.0);
    DBuffer2 = float4(0.0, 0.0, 0.0, 1.0);
#ifdef DECALS_4RT
    DBuffer3 = float2(1.0, 1.0);
#else
    float2 DBuffer3 = float2(1.0, 1.0);
#endif

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_DECAL, decalStart, decalCount);

    #if SCALARIZE_LIGHT_LOOP
    // Fast path is when we all pixels in a wave are accessing same tile or cluster.
    uint decalStartLane0 = WaveReadLaneFirst(decalStart);
    bool fastPath = WaveActiveAllTrue(decalStart == decalStartLane0);
    #endif

#else // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    decalCount = _DecalCount;
    decalStart = 0;
#endif

    float3 positionRWS = posInput.positionWS;

    // get world space ddx/ddy for adjacent pixels to be used later in mipmap lod calculation
    float3 positionRWSDdx = ddx(positionRWS);
    float3 positionRWSDdy = ddy(positionRWS);

    // Scalarized loop. All decals that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the ones relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that decal data accessed should be largely coherent
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_decalListOffset = 0;
    uint v_decalIdx = decalStart;
    while (v_decalListOffset < decalCount)
    {
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        v_decalIdx = FetchIndex(decalStart, v_decalListOffset);
#else
        v_decalIdx = decalStart + v_decalListOffset;
#endif // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER

        uint s_decalIdx = v_decalIdx;

#if SCALARIZE_LIGHT_LOOP

        if (!fastPath)
        {
            // If we are not in fast path, v_lightIdx is not scalar, so we need to query the Min value across the wave.
            s_decalIdx = WaveActiveMin(v_decalIdx);
            // If WaveActiveMin returns 0xffffffff it means that all lanes are actually dead, so we can safely ignore the loop and move forward.
            // This could happen as an helper lane could reach this point, hence having a valid v_lightIdx, but their values will be ignored by the WaveActiveMin
            if (s_decalIdx == -1)
            {
                break;
            }
        }
        // Note that the WaveReadLaneFirst should not be needed, but the compiler might insist in putting the result in VGPR.
        // However, we are certain at this point that the index is scalar.
        s_decalIdx = WaveReadLaneFirst(s_decalIdx);

#endif // SCALARIZE_LIGHT_LOOP

        DecalData s_decalData = FetchDecal(s_decalIdx);

        // If current scalar and vector decal index match, we process the decal. The v_decalListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_decalIdx value that is smaller than s_decalIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_decalIdx >= v_decalIdx)
        {
            v_decalListOffset++;
            EvalDecalMask(posInput, positionRWSDdx, positionRWSDdy, s_decalData, DBuffer0, DBuffer1, DBuffer2, DBuffer3, mask, alpha);
        }

    }
#else // _SURFACE_TYPE_TRANSPARENT
    #ifdef PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER
    int stride = (_ScreenSize.x + 7) / 8;
    int2 maskIndex = posInput.positionSS / 8;
    mask = _DecalPropertyMaskBufferSRV[stride * maskIndex.y + maskIndex.x];
    #else
    mask = DBUFFERHTILEBIT_DIFFUSE | DBUFFERHTILEBIT_NORMAL | DBUFFERHTILEBIT_MASK;
    #endif
#endif
    DecalSurfaceData decalSurfaceData;
    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
    decalSurfaceData.HTileMask = mask;

    return decalSurfaceData;
}
