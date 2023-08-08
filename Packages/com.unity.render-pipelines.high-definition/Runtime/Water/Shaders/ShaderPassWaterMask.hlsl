#if SHADERPASS != SHADERPASS_WATER_MASK
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/ShaderPassWaterCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

void Frag(PackedVaryingsToPS packedInput,
    out float4 outGBuffer0 : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

#ifdef DEBUG_DISPLAY
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    // Get the surface and built in data
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    #ifdef SHADER_STAGE_FRAGMENT
    bsdfData.frontFace = packedInput.cullFace;
    #endif

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);
    // Smoothness is modified based on camera distance
    surfaceData.perceptualSmoothness = 1.0 - bsdfData.perceptualRoughness;

    outGBuffer0 = float4(0.0, 0.0, 0.0, 0.0);
    bool viewMaterial = GetMaterialDebugColor(outGBuffer0, input, builtinData, posInput, surfaceData, bsdfData);

    if (!viewMaterial)
    {
        uint featureFlags = LIGHT_FEATURE_MASK_FLAGS; // we support everything for debug mode

        LightLoopOutput lightLoopOutput;
        LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

        outGBuffer0.xyz = (lightLoopOutput.diffuseLighting + lightLoopOutput.specularLighting) * GetCurrentExposureMultiplier();
    }
#else
    // World space position of the fragment
    float3 transformedPosAWS = GetAbsolutePositionWS(input.positionRWS);

    if (_WaterDebugMode == WATERDEBUGMODE_SIMULATION_FOAM_MASK)
    {
        float2 maskUV = EvaluateFoamMaskUV(input.texCoord0.xy);
        float foamMask = SAMPLE_TEXTURE2D(_SimulationFoamMask, sampler_SimulationFoamMask, maskUV).x;
        outGBuffer0 = float4(foamMask, foamMask, foamMask, 1.0);
    }
    else if (_WaterDebugMode == WATERDEBUGMODE_WATER_MASK)
    {
        float2 maskUV = EvaluateWaterMaskUV(input.texCoord0.xy);
        float3 waterMask = RemapWaterMaskValue(SAMPLE_TEXTURE2D(_WaterMask, sampler_WaterMask, maskUV).xyz);
        float3 debugMask = waterMask[_WaterMaskDebugMode];
        outGBuffer0 = float4(debugMask, 1.0);
    }
    else if (_WaterDebugMode == WATERDEBUGMODE_CURRENT)
    {
        // Grab the local direction
        #if defined(WATER_LOCAL_CURRENT)
        float2 dir;
        if (_WaterCurrentDebugMode == 0)
            dir = SampleWaterGroup0CurrentMap(input.texCoord0.xy);
        else
            dir = SampleWaterGroup1CurrentMap(input.texCoord0.xy);
        #else
        float2 dir = float2(1, 0);
        #endif

        // Apply the current orientation
        float sinC, cosC;
        sincos(_GroupOrientation[_WaterCurrentDebugMode], sinC, cosC);
        dir = float2(cosC * dir.x - sinC * dir.y, sinC * dir.x + cosC * dir.y);

        // Evaluate the tile size
        float2 tileSize = ARROW_TILE_SIZE / _CurrentDebugMultiplier;
        // Evaluate the arrow
        float arrowV = EvaluateArrow(input.texCoord0.xy, dir, tileSize);
        outGBuffer0 = float4((dir * 0.5 + 0.5) * (1.0 - arrowV), 0.0, 1.0);
    }
    else if (_WaterDebugMode == WATERDEBUGMODE_DEFORMATION)
    {
        if (_WaterDeformationExtent.x > 0.0)
        {
            // Define the sampling coordinates
            float2 deformationUV = (transformedPosAWS.xz - _WaterDeformationCenter) / _WaterDeformationExtent + 0.5;

            // Sample the deformation region
            float verticalDeformation = SAMPLE_TEXTURE2D_LOD(_WaterDeformationBuffer, s_linear_clamp_sampler, deformationUV, 0);

            // Evaluate the region flag
            float regionFlag = deformationUV.x > 0.0 && deformationUV.x < 1.0 && deformationUV.y > 0.0 && deformationUV.y < 1.0;
            float negativeDisplacement = max(-verticalDeformation, 0);
            float positiveDisplacement = max(verticalDeformation, 0);
            outGBuffer0 = float4(negativeDisplacement / (1.0 + negativeDisplacement), positiveDisplacement / (1.0 + positiveDisplacement), regionFlag, 1.0);
        }
        else
        {
            outGBuffer0 = float4(0.0, 0.0, 0.0, 1.0);
        }
    }
    else if (_WaterDebugMode == WATERDEBUGMODE_FOAM)
    {
        WaterAdditionalData waterAdditionalData;
        EvaluateWaterAdditionalData(input.texCoord0.xyy, input.positionRWS, float3(0, 1, 0), waterAdditionalData);

        float2 foamUV = EvaluateFoamUV(transformedPosAWS.xz);
        float foamRegionMask = all(foamUV > 0.0) && all(foamUV < 1.0) ? 1.0 : 0.0;
        float targetFoam = _WaterFoamDebugMode == 0 ? waterAdditionalData.surfaceFoam : waterAdditionalData.deepFoam;
        outGBuffer0 = float4(targetFoam, targetFoam, foamRegionMask, 1.0);
    }
    else
    {
        // Never suppsoed to run this code, display a magenta color to notify
        outGBuffer0 = float4(1.0, 0.0, 1.0, 1.0);
    }
#endif
}
