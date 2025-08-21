#ifndef UNDER_WATER_UTILITIES_H
#define UNDER_WATER_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDStencilUsage.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

// This file is included in various part of HDRP
// We don't want water specific global shader variables to leak into the global scope
#if defined(WATER_FOG_PASS) || defined(WATER_SURFACE_GBUFFER) || defined(WATER_ONE_BAND) || defined(WATER_TWO_BANDS) || defined(WATER_THREE_BANDS)
#define IS_HDRP_WATER_SYSTEM_PASS
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"
#endif

#if defined(_SURFACE_TYPE_TRANSPARENT) && defined(_ENABLE_FOG_ON_TRANSPARENT)
#define SUPPORT_WATER_ABSORPTION
#endif

// Buffers used for refraction sorting
#if defined(SUPPORT_WATER_ABSORPTION) || defined (_TRANSPARENT_REFRACTIVE_SORT)
TYPED_TEXTURE2D_X(uint2, _StencilTexture);
TEXTURE2D_X(_RefractiveDepthBuffer);
#endif

StructuredBuffer<float> _WaterCameraHeightBuffer;

float GetWaterCameraHeight()
{
    return _WaterCameraHeightBuffer[4 * unity_StereoEyeIndex];
}

StructuredBuffer<uint> _WaterLine;

float GetUnderWaterDistance(uint2 coord)
{
    float2 upVector = float2(_UpDirectionX, _UpDirectionY);
    float2 rightVector = float2(_UpDirectionY, -_UpDirectionX);

    // Find index to sample in 1D buffer
    uint xr = unity_StereoEyeIndex * _BufferStride;
    uint2 boundsX = uint2(0xFFFFFFFF - _WaterLine[0 + xr], _WaterLine[1 + xr]);
    uint posX = round(dot((float2)coord.xy, rightVector) - _BoundsSS.x);
    posX = clamp(posX, min(boundsX.y, boundsX.x), max(boundsX.x, boundsX.y));

    // Decompress water line height
    float posY = dot((float2)coord.xy, upVector) - _BoundsSS.z;
    uint packedValue = _WaterLine[posX + 2 + xr] & 0xFFFF;
    float waterLine = packedValue - 1;

    // Normalize distance to water line
    float maxHeight = (_BoundsSS.w - _BoundsSS.z);
    float distanceToWaterLine = (floor(posY) - waterLine) / maxHeight;

    return distanceToWaterLine;
}

bool IsUnderWater(uint2 coord)
{
    return GetUnderWaterDistance(coord) < 0.0f;
}

TEXTURE2D_X(_WaterGBufferTexture0);
TEXTURE2D_X(_WaterGBufferTexture1);
TEXTURE2D_X(_WaterGBufferTexture2);
TEXTURE2D_X(_WaterGBufferTexture3);
StructuredBuffer<WaterSurfaceProfile> _WaterSurfaceProfiles;

uint UnpackSurfaceIndex(uint2 positionSS)
{
    float4 inGBuffer3 = LOAD_TEXTURE2D_X(_WaterGBufferTexture3, positionSS);
    return ((uint)(inGBuffer3.w * 255.0f)) & 0xf;
}

uint GetWaterSurfaceIndex(uint2 positionSS)
{
    return IsUnderWater(positionSS) ? _UnderWaterSurfaceIndex : UnpackSurfaceIndex(positionSS);
}

Texture2D<float4> _WaterCausticsDataBuffer;

#if defined(IS_HDRP_WATER_SYSTEM_PASS) || defined(SUPPORT_WATER_ABSORPTION)
float EvaluateSimulationCaustics(float3 refractedWaterPosRWS, float refractedWaterDepth, float2 distortedWaterNDC)
{
    // We cannot have same variable names in different constant buffers, use some defines to select the correct ones
    #ifdef SUPPORT_WATER_ABSORPTION
    #define _CausticsIntensity          _UnderWaterCausticsIntensity
    #define _CausticsPlaneBlendDistance _UnderWaterCausticsPlaneBlendDistance
    #define _CausticsTilingFactor       _UnderWaterCausticsTilingFactor
    #define _CausticsRegionSize         _UnderWaterCausticsRegionSize
    #define _CausticsMaxLOD             _UnderWaterCausticsMaxLOD
    #define _WaterSurfaceTransform_Inverse _UnderWaterSurfaceTransform_Inverse
    #endif

    float caustics = 0.0f;

    // TODO: Is this worth a multicompile?
    if (_CausticsIntensity != 0.0f)
    {
        // Evaluate the caustics weight
        float causticWeight = saturate(refractedWaterDepth / _CausticsPlaneBlendDistance);

        // Evaluate the normal of the surface (using partial derivatives of the absolute world pos is not possible as it is not stable enough)
        NormalData normalData;
        float4 normalBuffer = LOAD_TEXTURE2D_X_LOD(_NormalBufferTexture, distortedWaterNDC * _ScreenSize.xy, 0);
        DecodeFromNormalBuffer(normalBuffer, normalData);

        // Evaluate the triplanar weights
        float3 normalOS = mul((float3x3)_WaterSurfaceTransform_Inverse, normalData.normalWS);
        float3 triplanarW = ComputeTriplanarWeights(normalOS);

        // Convert the position to absolute world space and move the position to the water local space
        float3 causticPosAWS = GetAbsolutePositionWS(refractedWaterPosRWS) * _CausticsTilingFactor;
        float3 causticPosOS = mul(_WaterSurfaceTransform_Inverse, float4(causticPosAWS, 1.0f)).xyz;

        // Evaluate the triplanar coodinates
        float3 sampleCoord = causticPosOS / (_CausticsRegionSize * 0.5) + 0.5;
        float2 uv0, uv1, uv2;
        GetTriplanarCoordinate(sampleCoord, uv0, uv1, uv2);

        // Evaluate the sharpness of the caustics based on the depth
        float sharpness = (1.0 - causticWeight) * _CausticsMaxLOD;

        // sample the caustics texture
        float3 causticsValues;
        #if defined(SHADER_STAGE_COMPUTE)
        causticsValues.x = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
        #else
        causticsValues.x = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
        #endif

        caustics = dot(causticsValues, triplanarW.yzx) * causticWeight * _CausticsIntensity;
    }

    #ifdef SUPPORT_WATER_ABSORPTION
    #undef _CausticsIntensity
    #undef _CausticsPlaneBlendDistance
    #undef _CausticsTilingFactor
    #undef _CausticsRegionSize
    #undef _CausticsMaxLOD
    #undef _WaterSurfaceTransform_Inverse
    #endif

    // Evaluate the triplanar weights and blend the samples togheter
    return 1.0 + caustics;
}
#endif

#ifdef SUPPORT_WATER_ABSORPTION
bool EvaluateUnderwaterAbsorption(PositionInputs posInput, out bool underWater, inout float3 opacity)
{
    #ifdef _ENABLE_FOG_ON_TRANSPARENT
    // Disable underwater on low res transparents
    if (_OffScreenRendering == 1)
    {
        underWater = false;
        return false;
    }
    #endif

    uint surfaceIndex = -1;
    bool hasWater = false, hasExcluder = false;
    float waterDepth = UNITY_RAW_FAR_CLIP_VALUE;
    underWater = IsUnderWater(posInput.positionSS.xy);
    bool skipFog = false;

    #ifdef _ENABLE_FOG_ON_TRANSPARENT
    [branch]
    if (_PreRefractionPass != 0)
    #endif
    {
        waterDepth = LOAD_TEXTURE2D_X(_RefractiveDepthBuffer, posInput.positionSS.xy).r;
        uint stencil = GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, posInput.positionSS.xy));
        hasExcluder = (stencil & STENCILUSAGE_WATER_EXCLUSION) != 0;
        hasWater = (stencil & STENCILUSAGE_WATER_SURFACE) != 0;

        float cameraToWater = underWater ? FLT_MAX : (hasWater ? waterDepth : 0.0f);
        float cameraToSurface = (underWater && hasWater && waterDepth >= posInput.deviceDepth) ? FLT_MAX : posInput.deviceDepth;
        surfaceIndex = cameraToSurface < cameraToWater ? GetWaterSurfaceIndex(posInput.positionSS.xy) : -1;

        #ifdef SUPPORT_WATER_ABSORPTION
        // Don't apply underwater fog on excluders
        // Still apply on transparents even if an excluder is behind though
        if (hasExcluder)
            surfaceIndex = -1;
        #endif

        if (surfaceIndex == -1)
            underWater = false;
        // Don't apply volumetric fog when viewing surface from above
        else if (!underWater)
        {
            WaterSurfaceProfile prof = _WaterSurfaceProfiles[surfaceIndex];
            PositionInputs waterPosInput = GetPositionInput(posInput.positionSS.xy, _ScreenSize.zw, waterDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
            float3 opticalDepth = 2 * length(posInput.positionWS - waterPosInput.positionWS) * prof.extinction;
            opacity = 1 - TransmittanceFromOpticalDepth(opticalDepth);
            skipFog = true;
        }
    }

    return skipFog;
}
#endif

#ifdef _TRANSPARENT_REFRACTIVE_SORT
// This function is used only by pre refraction transparent objects (which includes volumetric clouds)
// It checks wether it's in front or behind a refractive object to output to the correct buffer for sorting
void ComputeRefractionSplitColor(PositionInputs posInput, inout float4 outColor, inout float4 outBeforeRefractionColor, inout float4 outBeforeRefractionAlpha)
{
    const uint refractiveMask = STENCILUSAGE_REFRACTIVE | STENCILUSAGE_WATER_SURFACE;
    bool hasRefractive = (GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, posInput.positionSS.xy)) & refractiveMask) != 0;
    float refractiveDepth = LOAD_TEXTURE2D_X(_RefractiveDepthBuffer, posInput.positionSS.xy).r;
    bool beforeRefraction = hasRefractive && posInput.deviceDepth >= refractiveDepth; // pixels between refractive object and camera

    // Perform per pixel sorting
    if (beforeRefraction)
    {
        outBeforeRefractionColor = outColor;
        outBeforeRefractionAlpha = float4(0, 0, 0, outColor.a);
        outColor            = 0;

        // Because we don't have control over shader blend mode (to be compatible with VT and MV), we handle blending manually
        if (_BlendMode == BLENDINGMODE_ADDITIVE)
            outBeforeRefractionColor.a = outBeforeRefractionAlpha.a = 0;
    }
    else
    {
        outBeforeRefractionColor = 0;
        outBeforeRefractionAlpha = 0;
    }
}
#endif

#endif // UNDER_WATER_UTILITIES_H
