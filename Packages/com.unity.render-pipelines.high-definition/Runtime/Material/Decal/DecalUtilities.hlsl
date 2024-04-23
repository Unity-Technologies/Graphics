#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"
#if defined(PATH_TRACING_CLUSTERED_DECALS)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingLightCluster.hlsl"
#else
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#endif
#ifndef SURFACE_GRADIENT
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#endif

DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

#define USE_CLUSTERED_DECALLIST ((defined(_SURFACE_TYPE_TRANSPARENT) && defined(HAS_LIGHTLOOP)) || defined(WATER_SURFACE_GBUFFER) || defined(PATH_TRACING_CLUSTERED_DECALS))

float ComputeDecalTextureLOD(float2 dpdx, float2 dpdy)
{
    float lod = ComputeTextureLOD(dpdx, dpdy, _DecalAtlasResolution, 0.5);
#if defined(SHADEROPTIONS_GLOBAL_MIP_BIAS) && (SHADEROPTIONS_GLOBAL_MIP_BIAS != 0)
    lod += _GlobalMipBias;
#endif
#if (SHADERPASS != SHADERPASS_PATH_TRACING) && defined(PATH_TRACING_CLUSTERED_DECALS)
    lod += _RayTracingLodBias; // Because SAMPLE_TEXTURE2D_LOD doesn't take this into account
#endif
    return lod;
}

#if USE_CLUSTERED_DECALLIST

// In order that the lod for with transpartent decal better match the lod for opaque decal
// We use ComputeTextureLOD with bias == 0.5
void EvalDecalMask( PositionInputs posInput, float3 vtxNormal, float3 positionRWSDdx, float3 positionRWSDdy, DecalData decalData,
                    inout float4 DBuffer0, inout float4 DBuffer1, inout float4 DBuffer2, inout float2 DBuffer3, inout float alpha)
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

        // need to compute the mipmap LOD manually because we are sampling inside a loop
        float3 positionDSDdx = mul(worldToDecal, float4(positionRWSDdx, 0.0)).xyz; // transform the derivatives to decal space, any translation is irrelevant
        float3 positionDSDdy = mul(worldToDecal, float4(positionRWSDdy, 0.0)).xyz;

        // Following code match the code in DecalData.hlsl used for DBuffer. It have the same kind of condition and similar code structure
        uint affectFlags = (int)(decalData.blendParams.z + 0.5f); // 1 albedo, 2 normal, 4 metal, 8 AO, 16 smoothness
        float fadeFactor = decalData.normalToWorld[0][3];
        // Check if this decal projector require angle fading
        float2 angleFade = float2(decalData.normalToWorld[1][3], decalData.normalToWorld[2][3]);

        // Angle fade is disabled if decal layers isn't enabled for consistency with DBuffer Decal
        // The test against _EnableDecalLayers is done here to refresh realtime as AngleFade is cached data and need a decal refresh to be updated.
        if (angleFade.x > 0.0f && _EnableDecalLayers) // if angle fade is enabled
        {
            float3 decalNormal = float3(decalData.normalToWorld[0].z, decalData.normalToWorld[1].z, decalData.normalToWorld[2].z);
            fadeFactor *= DecodeAngleFade(dot(vtxNormal, decalNormal), angleFade);
        }

        float albedoMapBlend;
        float maskMapBlend = fadeFactor * decalData.scalingBlueMaskMap;

        // Albedo
        // We must always sample diffuse texture due to opacity that can affect everything)
        {
            float4 src = decalData.baseColor;

            // We use scaleBias value to now if we have init a texture. 0 mean a texture is bound
            bool diffuseTextureBound = (decalData.diffuseScaleBias.x > 0) && (decalData.diffuseScaleBias.y > 0);
            if (diffuseTextureBound)
            {
                // Caution: We can't compute LOD inside a dynamic loop. The gradient are not accessible.
                float2 diffuseMin = decalData.diffuseScaleBias.zw + clampAmount;                                    // offset into atlas is in .zw
                float2 diffuseMax = decalData.diffuseScaleBias.zw + decalData.diffuseScaleBias.xy - clampAmount;    // scale relative to full atlas size is in .xy so total texture extent in atlas is (1,1) * scale

                float2 sampleDiffuse = clamp(positionDS.xz * decalData.diffuseScaleBias.xy + decalData.diffuseScaleBias.zw, diffuseMin, diffuseMax);
                float2 sampleDiffuseDdx = positionDSDdx.xz * decalData.diffuseScaleBias.xy; // factor in the atlas scale
                float2 sampleDiffuseDdy = positionDSDdy.xz * decalData.diffuseScaleBias.xy;
                float  lodDiffuse = ComputeDecalTextureLOD(sampleDiffuseDdx, sampleDiffuseDdy);

                src *= SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, sampleDiffuse, lodDiffuse);
            }

            src.w *= fadeFactor;
            albedoMapBlend = src.w;  // diffuse texture alpha affects all other channels

            // Accumulate in dbuffer (mimic what ROP are doing)
            DBuffer0.xyz = (affectFlags & 1) ? src.xyz * src.w + DBuffer0.xyz * (1.0 - src.w) : DBuffer0.xyz; // Albedo
            DBuffer0.w = (affectFlags & 1) ? DBuffer0.w * (1.0 - src.w) : DBuffer0.w; // Albedo alpha

            // Specific to transparent and requested by the artist: use decal alpha if it is higher than transparent alpha
            alpha = max(alpha, albedoMapBlend);
        }

        // Metal/ao/smoothness - 28 -> 1C
        #ifdef DECALS_4RT
        if (affectFlags & 0x1C)
        #else // only smoothness in 3RT mode
        if (affectFlags & 0x10)
        #endif
        {
            float4 src;

            // We use scaleBias value to now if we have init a texture. 0 mean a texture is bound
            bool maskTextureBound = (decalData.maskScaleBias.x > 0) && (decalData.maskScaleBias.y > 0);
            if (maskTextureBound)
            {
                // Caution: We can't compute LOD inside a dynamic loop. The gradient are not accessible.
                float2 maskMin = decalData.maskScaleBias.zw + clampAmount;
                float2 maskMax = decalData.maskScaleBias.zw + decalData.maskScaleBias.xy - clampAmount;

                float2 sampleMask = clamp(positionDS.xz * decalData.maskScaleBias.xy + decalData.maskScaleBias.zw, maskMin, maskMax);

                float2 sampleMaskDdx = positionDSDdx.xz * decalData.maskScaleBias.xy;
                float2 sampleMaskDdy = positionDSDdy.xz * decalData.maskScaleBias.xy;
                float  lodMask = ComputeDecalTextureLOD(sampleMaskDdx, sampleMaskDdy);

                src = SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, sampleMask, lodMask);
                maskMapBlend *= src.z; // store before overwriting with smoothness
                #ifdef DECALS_4RT
                src.x = lerp(decalData.remappingMetallic.x, decalData.remappingMetallic.y, src.x); // Remap Metal
                src.y = lerp(decalData.remappingAOS.x, decalData.remappingAOS.y, src.y); // Remap AO
                #endif
                src.z = lerp(decalData.remappingAOS.z, decalData.remappingAOS.w, src.w); // Remap Smoothness
            }
            else
            {
                #ifdef DECALS_4RT
                src.x = decalData.remappingMetallic.x; // Metal
                src.y = decalData.remappingAOS.x; // AO
                #endif
                src.z = decalData.remappingAOS.z; // Smoothness
            }

            src.w = (decalData.blendParams.y == 1.0) ? maskMapBlend : albedoMapBlend;

            // Accumulate in dbuffer (mimic what ROP are doing)
            #ifdef DECALS_4RT
            DBuffer2.x = (affectFlags & 4) ? src.x * src.w + DBuffer2.x * (1.0 - src.w) : DBuffer2.x; // Metal
            DBuffer3.x = (affectFlags & 4) ? DBuffer3.x * (1.0 - src.w) : DBuffer3.x; // Metal alpha

            DBuffer2.y = (affectFlags & 8) ? src.y * src.w + DBuffer2.y * (1.0 - src.w) : DBuffer2.y; // AO
            DBuffer3.y = (affectFlags & 8) ? DBuffer3.y * (1.0 - src.w) : DBuffer3.y; // AO alpha
            #endif
            DBuffer2.z = (affectFlags & 16) ? src.z * src.w + DBuffer2.z * (1.0 - src.w) : DBuffer2.z; // Smoothness
            DBuffer2.w = (affectFlags & 16) ? DBuffer2.w * (1.0 - src.w) : DBuffer2.w; // Smoothness alpha
        }

        // Normal
        if (affectFlags & 2)
        {
            float4 src = float4(0.0, 0.0, 0.0, 0.0);
            float3 normalTS = float3(0.0, 0.0, 1.0);
            float normalAlpha = 0.0f;
            
            // We use scaleBias value to now if we have init a texture. 0 mean a texture is bound
            bool normalTextureBound = (decalData.normalScaleBias.x > 0) && (decalData.normalScaleBias.y > 0);
            if (normalTextureBound)
            {
                // Caution: We can't compute LOD inside a dynamic loop. The gradient are not accessible.
                float2 normalMin = decalData.normalScaleBias.zw + clampAmount;
                float2 normalMax = decalData.normalScaleBias.zw + decalData.normalScaleBias.xy - clampAmount;
                float2 sampleNormal = clamp(positionDS.xz * decalData.normalScaleBias.xy + decalData.normalScaleBias.zw, normalMin, normalMax);
                float2 sampleNormalDdx = positionDSDdx.xz * decalData.normalScaleBias.xy;
                float2 sampleNormalDdy = positionDSDdy.xz * decalData.normalScaleBias.xy;
                float  lodNormal = ComputeDecalTextureLOD(sampleNormalDdx, sampleNormalDdy);

                real4 atlasData = SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, sampleNormal, lodNormal);
                normalAlpha = atlasData.b;

                #ifdef DECAL_SURFACE_GRADIENT
                float3x3 tangentToWorld = transpose((float3x3)decalData.normalToWorld);
                float2 deriv = UnpackDerivativeNormalRGorAG(atlasData);
                src.xyz = SurfaceGradientFromTBN(deriv, tangentToWorld[0], tangentToWorld[1]);
                #else
                normalTS = UnpackNormalmapRGorAG(atlasData);
                #endif
            }

            #ifndef DECAL_SURFACE_GRADIENT
            src.xyz = mul((float3x3)decalData.normalToWorld, normalTS);
            #else
            src.xyz *= -1; // see EncodeIntoDBuffer for why we flip
            #endif

            src.xyz = src.xyz * 0.5 + 0.5; // Mimic what is happening when calling EncodeIntoDBuffer()
            bool normalMapAlpha = decalData.sampleNormalAlpha == 1.0f;
            if (normalMapAlpha)
                src.w = normalAlpha;
            else
                src.w = (decalData.blendParams.x == 1.0) ? maskMapBlend : albedoMapBlend;

            // Accumulate in dbuffer (mimic what ROP are doing)
            DBuffer1.xyz = src.xyz * src.w + DBuffer1.xyz * (1.0 - src.w);
            DBuffer1.w = DBuffer1.w * (1.0 - src.w);
        }
    }
}

DecalData FetchDecal(uint index)
{
    return _DecalDatas[index];
}

void GetDecalSurfaceDataFromCluster(PositionInputs posInput, float3 vtxNormal, uint meshRenderingDecalLayer, inout float alpha, out DecalSurfaceData decalSurfaceData)
{
    DBufferType0 DBuffer0 = float4(0.0, 0.0, 0.0, 1.0);
    DBufferType1 DBuffer1 = float4(0.5, 0.5, 0.5, 1.0);
    DBufferType2 DBuffer2 = float4(0.0, 0.0, 0.0, 1.0);
#ifdef DECALS_4RT
    DBufferType3 DBuffer3 = float2(1.0, 1.0);
#else
    float2 DBuffer3 = float2(1.0, 1.0);
#endif

    uint decalCount = _DecalCount;
    uint decalStart = 0;

#if defined(PATH_TRACING_CLUSTERED_DECALS)
    uint decalEnd, cellIndex;
    GetLightCountAndStartCluster(posInput.positionWS, LIGHTCATEGORY_DECAL, decalStart, decalEnd, cellIndex);
        
    decalCount = decalEnd - decalStart;
    // we disable fast path scalarization in the path tracer due to PS5 compilation issues
    uint decalStartLane0 = cellIndex;
    bool fastPath = false;
#elif !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && USE_CLUSTERED_DECALLIST
    GetCountAndStart(posInput, LIGHTCATEGORY_DECAL, decalStart, decalCount);

    // Fast path is when we all pixels in a wave are accessing same tile or cluster.
    uint decalStartLane0;
    bool fastPath = IsFastPath(decalStart, decalStartLane0);
#else
    bool fastPath = true;
#endif

    float3 positionRWS = posInput.positionWS;

    // get world space ddx/ddy for adjacent pixels to be used later in mipmap lod calculation
#if defined(PATH_TRACING_CLUSTERED_DECALS)
    float3 positionRWSDdx = 0;
    float3 positionRWSDdy = 0;
#else
    float3 positionRWSDdx = ddx(positionRWS);
    float3 positionRWSDdy = ddy(positionRWS);
#endif 

    // Scalarized loop. All decals that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the ones relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that decal data accessed should be largely coherent
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_decalListOffset = 0;
    uint v_decalIdx = decalStart;
#if NEED_TO_CHECK_HELPER_LANE
    // On some platform helper lanes don't behave as we'd expect, therefore we prevent them from entering the loop altogether.
    bool isHelperLane = WaveIsHelperLane();
    while (!isHelperLane && v_decalListOffset < decalCount)
#else
    while (v_decalListOffset < decalCount)
#endif
    {
#if defined(PATH_TRACING_CLUSTERED_DECALS)
        v_decalIdx = GetLightClusterCellLightByIndex(cellIndex, decalStart + v_decalListOffset);
#else
        v_decalIdx = FetchIndex(decalStart, v_decalListOffset);
#endif

        uint s_decalIdx = ScalarizeElementIndex(v_decalIdx, fastPath);
        if (s_decalIdx == -1)
            break;

        // If current scalar and vector decal index match, we process the decal. The v_decalListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_decalIdx value that is smaller than s_decalIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_decalIdx >= v_decalIdx)
        {
            v_decalListOffset++;

            DecalData s_decalData = FetchDecal(s_decalIdx);
            bool isRejected = _EnableDecalLayers && (s_decalData.decalLayerMask & meshRenderingDecalLayer) == 0;
            if (!isRejected)
                EvalDecalMask(posInput, vtxNormal, positionRWSDdx, positionRWSDdy, s_decalData, DBuffer0, DBuffer1, DBuffer2, DBuffer3, alpha);
        }
    }

    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
}
#endif // USE_CLUSTERED_DECALLIST

void GetDecalSurfaceDataFromDBuffer(PositionInputs posInput, out DecalSurfaceData decalSurfaceData)
{
    FETCH_DBUFFER(DBuffer, _DBufferTexture, int2(posInput.positionSS.xy));
    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
}

DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, float3 vtxNormal, uint meshRenderingDecalLayer, inout float alpha)
{
    DecalSurfaceData decalSurfaceData;
    
#if USE_CLUSTERED_DECALLIST // forward transparent, deferred water, and raytracing use clustered decals
    GetDecalSurfaceDataFromCluster(posInput, vtxNormal, meshRenderingDecalLayer, alpha, decalSurfaceData);
#else // Opaque - use DBuffer
    GetDecalSurfaceDataFromDBuffer(posInput, decalSurfaceData);
#endif

    return decalSurfaceData;
}

DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, FragInputs input, uint decalLayer, inout float alpha)
{
    float3 vtxNormal = input.tangentToWorld[2];
    DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, vtxNormal, decalLayer, alpha);

#if !defined(DECAL_SURFACE_GRADIENT) && defined(_DOUBLESIDED_ON)
    // 'doubleSidedConstants' is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode.
    // It's float3(1, 1, 1) in the none mode.
    float flipSign = input.isFrontFace ? 1.0 : _DoubleSidedConstants.x;
    decalSurfaceData.normalWS.xy *= flipSign;
#endif

    return decalSurfaceData;
}

DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, FragInputs input, inout float alpha)
{
    return GetDecalSurfaceData(posInput, input, GetMeshRenderingLayerMask(), alpha);
}

// There are two variants of this function depending on if we are using surface gradients or not
void ApplyDecalToSurfaceNormal(DecalSurfaceData decalSurfaceData, inout float3 normalWS)
{
    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0)
        normalWS.xyz = SafeNormalize(normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
}

void ApplyDecalToSurfaceNormal(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout float3 normalTS)
{
    // Always test the normal as we can have decompression artifact
    float3 addValue = float3(0.0,0.0,0.0);
    if (decalSurfaceData.normalWS.w < 1.0)
    {
        addValue = SurfaceGradientFromVolumeGradient (vtxNormal, decalSurfaceData.normalWS.xyz);
    }
    normalTS += addValue;
}
