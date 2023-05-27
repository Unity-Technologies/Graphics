#ifndef UNDER_WATER_UTILITIES_H
#define UNDER_WATER_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDStencilUsage.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

TEXTURE2D_X_UINT2(_StencilTexture);
StructuredBuffer<float> _WaterCameraHeightBuffer;

float GetWaterCameraHeight()
{
    return _WaterCameraHeightBuffer[unity_StereoEyeIndex];
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
    posX = clamp(posX, boundsX.x, boundsX.y);

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

float EvaluateSimulationCaustics(float3 refractedWaterPosRWS, float refractedWaterDepth, float2 distortedWaterNDC)
{
    // Will hold the results of the caustics evaluation
    float3 causticsValues = 0.0;
    float3 triplanarW = 0.0;
    float causticWeight = 0.0;

    // We cannot have same variable names in different constant buffers, use some defines to select the correct ones
    #if defined(_TRANSPARENT_REFRACTIVE_SORT) || defined(SUPPORT_WATER_ABSORPTION)
    #define _CausticsIntensity          _UnderWaterCausticsIntensity
    #define _CausticsPlaneBlendDistance _UnderWaterCausticsPlaneBlendDistance
    #define _WaterProceduralGeometry    _UnderWaterProceduralGeometry
    #define _WaterSurfaceTransform_Inverse _UnderWaterSurfaceTransform_Inverse
    #define _CausticsTilingFactor       _UnderWaterCausticsTilingFactor
    #define _CausticsRegionSize         _UnderWaterCausticsRegionSize
    #define _CausticsMaxLOD             _UnderWaterCausticsMaxLOD
    #endif

    // TODO: Is this worth a multicompile?
    if (_CausticsIntensity != 0.0f)
    {
        // Evaluate the caustics weight
        causticWeight = saturate(refractedWaterDepth / _CausticsPlaneBlendDistance);

        // Evaluate the normal of the surface (using partial derivatives of the absolute world pos is not possible as it is not stable enough)
        NormalData normalData;
        float4 normalBuffer = LOAD_TEXTURE2D_X_LOD(_NormalBufferTexture, distortedWaterNDC * _ScreenSize.xy, 0);
        DecodeFromNormalBuffer(normalBuffer, normalData);

        // Evaluate the triplanar weights
        triplanarW = ComputeTriplanarWeights(_WaterProceduralGeometry ? mul(_WaterSurfaceTransform_Inverse, float4(normalData.normalWS, 0.0)).xyz : normalData.normalWS);

        // Convert the position to absolute world space and move the position to the water local space
        float3 causticPosOS = GetAbsolutePositionWS(refractedWaterPosRWS) * _CausticsTilingFactor;
        causticPosOS = _WaterProceduralGeometry ? mul(_WaterSurfaceTransform_Inverse, float4(causticPosOS, 1.0)).xyz : causticPosOS;

        // Evaluate the triplanar coodinates
        float3 sampleCoord = causticPosOS / (_CausticsRegionSize * 0.5) + 0.5;
        float2 uv0, uv1, uv2;
        GetTriplanarCoordinate(sampleCoord, uv0, uv1, uv2);

        // Evaluate the sharpness of the caustics based on the depth
        float sharpness = (1.0 - causticWeight) * _CausticsMaxLOD;

        // sample the caustics texture
        #if defined(SHADER_STAGE_COMPUTE)
        causticsValues.x = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
        #else
        causticsValues.x = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
        #endif
    }

    // Evaluate the triplanar weights and blend the samples togheter
    return 1.0 + lerp(0, causticsValues.x * triplanarW.y
            + causticsValues.y * triplanarW.z
            + causticsValues.z * triplanarW.x, causticWeight) * _CausticsIntensity;
}

#if defined(_TRANSPARENT_REFRACTIVE_SORT) || defined(SUPPORT_WATER_ABSORPTION) || defined(_ENABLE_FOG_ON_TRANSPARENT)

TEXTURE2D_X(_RefractiveDepthBuffer);

bool EvaluateUnderwaterAbsorption(PositionInputs posInput, out float3 color, out float3 opacity)
{
    color = opacity = 0;

    bool underWater = IsUnderWater(posInput.positionSS.xy);
    bool hasWater = (GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, posInput.positionSS.xy)) & STENCILUSAGE_WATER_SURFACE) != 0;

    float waterDepth = LOAD_TEXTURE2D_X(_RefractiveDepthBuffer, posInput.positionSS.xy).r;
    PositionInputs waterPosInput = GetPositionInput(posInput.positionSS.xy, _ScreenSize.zw, waterDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    waterDepth = waterPosInput.linearDepth;

    float cameraToWater = underWater ? 0.0f : (hasWater ? waterDepth : FLT_MAX);
    float cameraToSurface = underWater && (hasWater && waterDepth < posInput.linearDepth) ? 0.0f : posInput.linearDepth;
    float absorptionDistance = cameraToSurface - cameraToWater;
    uint surfaceIndex = absorptionDistance > 0.0f ? GetWaterSurfaceIndex(posInput.positionSS.xy) : -1;

    if (surfaceIndex != -1)
    {
        float caustics = 1.0f;
        float3 farColor = 0.0f;
        WaterSurfaceProfile prof = _WaterSurfaceProfiles[surfaceIndex];

        if (underWater)
        {
            // Approximate the pixel depth based on the distance from camera to surface
            float distanceToSurface = max(-dot(posInput.positionWS, prof.upDirection) - GetWaterCameraHeight(), 0);
            absorptionDistance = length(posInput.positionWS) + distanceToSurface;

            // Apply underwater post process modifiers
            absorptionDistance *= prof.absorptionDistanceMultiplier;

            if (!hasWater) // caustics on pixels with water are applied during gbuffer pass
            {
                #ifdef SUPPORT_WATER_CAUSTICS 
                caustics = EvaluateSimulationCaustics(posInput.positionWS, distanceToSurface, posInput.positionNDC.xy);
                #endif

                #ifdef SUPPORT_WATER_CAUSTICS_SHADOW 
                if (_DirectionalShadowIndex >= 0) // In case the user asked for shadow to explicitly be affected by shadows
                {
                    DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];
                    if ((light.lightDimmer > 0) && (light.shadowDimmer > 0))
                    {
                        float3 L = -light.forward;
                        HDShadowContext ctx = InitShadowContext();
                        float sunShadow = GetDirectionalShadowAttenuation(ctx, posInput.positionSS, posInput.positionWS, L, light.shadowIndex, L);
                        caustics = 1 + (caustics - 1) * lerp(_UnderWaterCausticsShadowIntensity, 1.0, sunShadow);
                    }
                }
                #endif
            }

            float ambient = _UnderWaterAmbientProbeLuminance * GetCurrentExposureMultiplier();
            farColor = prof.scatteringColor * lerp(1.0, ambient, prof.underWaterAmbientProbeContribution);
        }
        else
        {
            // Approximate the pixel depth using distance from camera to object (light travels back and forth)
            absorptionDistance = 2 * length(posInput.positionWS - waterPosInput.positionWS);
        }

        float3 absorptionTint = exp(-absorptionDistance * prof.outScatteringCoefficient * (1.f - prof.transparencyColor));

        color = farColor * (1 - absorptionTint);
        opacity = 1 - caustics * absorptionTint;
    }

    return surfaceIndex != -1;
}
#endif

#ifdef _ENABLE_FOG_ON_TRANSPARENT
float4 ComputeFog(PositionInputs posInput, float3 V, float4 outColor)
{
    // Evaluate water absorption or atmospheric scattering
    // Check _OffScreenRendering to disable underwater on low res transparents
    float3 volColor, volOpacity;
    if (_EnableWater == 0 || _OffScreenRendering == 1 || !EvaluateUnderwaterAbsorption(posInput, volColor, volOpacity))
        EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);
    return ApplyFogOnTransparent(outColor, volColor, volOpacity);
}
#endif

#ifdef _TRANSPARENT_REFRACTIVE_SORT
// This function is used only by pre refraction transparent objects
// It checks wether it's in front or behind a refractive object to output to the correct buffer for sorting
void ComputeRefractionSplitColor(PositionInputs posInput, float3 V, inout float4 outColor, inout float4 outBeforeRefractionColor, inout float4 outBeforeRefractionAlpha)
{
    const uint refractiveMask = STENCILUSAGE_REFRACTIVE | STENCILUSAGE_WATER_SURFACE;
    bool hasRefractive = (GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, posInput.positionSS.xy)) & refractiveMask) != 0;
    float refractiveDepth = LOAD_TEXTURE2D_X(_RefractiveDepthBuffer, posInput.positionSS.xy).r;
    bool beforeRefraction = hasRefractive && posInput.deviceDepth >= refractiveDepth; // pixels between refractive object and camera

    #ifdef _ENABLE_FOG_ON_TRANSPARENT
    outColor = ComputeFog(posInput, V, outColor);
    #endif

    // Perform per pixel sorting
    if (beforeRefraction)
    {
        outBeforeRefractionColor = outColor;
        outBeforeRefractionAlpha = float4(0, 0, 0, outColor.a);
        outColor            = 0;

        // Because we don't have control over shader blend mode (to be compatible with VT and MV), we handle blending manually
        if (_BlendMode == BLENDMODE_ADDITIVE)
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
