#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
#error SHADERPASS_is_not_correctly_define
#endif

#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/MotionVectorVertexShaderCommon.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh, AttributesPass inputPass)
{
    VaryingsType varyingsType;
#ifdef HAVE_VFX_MODIFICATION
    AttributesElement inputElement;
    varyingsType.vmesh = VertMesh(inputMesh, inputElement);
    return MotionVectorVS(varyingsType, inputMesh, inputPass, inputElement);
#else
    varyingsType.vmesh = VertMesh(inputMesh);
    return MotionVectorVS(varyingsType, inputMesh, inputPass);
#endif
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return MotionVectorTessellation(output, input);
}

#endif // TESSELLATION_ON

#else // _WRITE_TRANSPARENT_MOTION_VECTOR

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#endif // TESSELLATION_ON

#endif // _WRITE_TRANSPARENT_MOTION_VECTOR

#ifdef TESSELLATION_ON
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

//NOTE: some shaders set target1 to be
//   Blend 1 SrcAlpha OneMinusSrcAlpha
//The reason for this blend mode is to let virtual texturing alpha dither work.
//Anything using Target1 should write 1.0 or 0.0 in alpha to write / not write into the target.
#ifdef UNITY_VIRTUAL_TEXTURING
    #define VT_BUFFER_TARGET SV_Target1
    #define EXTRA_BUFFER_TARGET SV_Target2
    #if defined(SHADER_API_PSSL)
        //For exact packing on pssl, we want to write exact 16 bit unorm (respect exact bit packing).
        //In some sony platforms, the default is FMT_16_ABGR, which would incur in loss of precision.
        //Thus, when VT is enabled, we force FMT_32_ABGR
        #pragma PSSL_target_output_format(target 1 FMT_32_ABGR)
    #endif
#else
    #define EXTRA_BUFFER_TARGET SV_Target1
#endif

float GetDeExposureMultiplier()
{
#if defined(DISABLE_UNLIT_DEEXPOSURE)
    return 1.0;
#else
    return _DeExposureMultiplier;
#endif
}

void Frag(PackedVaryingsToPS packedInput,
            out float4 outColor : SV_Target0
        #ifdef UNITY_VIRTUAL_TEXTURING
            ,out float4 outVTFeedback : VT_BUFFER_TARGET
        #endif
        #ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
            , out float4 outMotionVec : EXTRA_BUFFER_TARGET
        #endif
        #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : DEPTH_OFFSET_SEMANTIC
        #endif
)
{
#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
    // Init outMotionVector here to solve compiler warning (potentially unitialized variable)
    // It is init to the value of forceNoMotion (with 2.0)
    // Always write 1.0 in alpha since blend mode could be active on this target as a side effect of VT feedback buffer
    // motion vector expected output format is RG16
    outMotionVec = float4(2.0, 0.0, 0.0, 1.0);
#endif

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    AdjustFragInputsToOffScreenRendering(input, _OffScreenRendering > 0, _OffScreenDownsampleFactor);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // Not lit here (but emissive is allowed)
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    // If this is a shadow matte, then we want the AO to affect the base color (the AO being correct if the surface is flagged shadow matte).
#if defined(_ENABLE_SHADOW_MATTE)
    bsdfData.color *= GetScreenSpaceAmbientOcclusion(input.positionSS.xy);
#endif

#ifdef DEBUG_DISPLAY
    // Handle debug lighting mode here as there is no lightloop for unlit.
    // For unlit we let all unlit object appear
    if (_DebugLightingMode >= DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING && _DebugLightingMode <= DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING)
    {
        if (_DebugLightingMode != DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING)
        {
            builtinData.emissiveColor = 0.0;
        }
        else
        {
            bsdfData.color = 0.0;
        }
    }
#endif

    // Note: we must not access bsdfData in shader pass, but for unlit we make an exception and assume it should have a color field
    float4 outResult = ApplyBlendMode(bsdfData.color * GetDeExposureMultiplier() + builtinData.emissiveColor * GetCurrentExposureMultiplier(), builtinData.opacity);
    outResult = EvaluateAtmosphericScattering(posInput, V, outResult);

#ifdef DEBUG_DISPLAY
    float4 debugColor = 0;
    if (GetMaterialDebugColor(debugColor, input, builtinData, posInput, surfaceData, bsdfData))
    {
        outResult = debugColor;
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
    {
        float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
        outResult = result;
    }
#endif

    outColor = outResult;

#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
    VaryingsPassToPS inputPass = UnpackVaryingsPassToPS(packedInput.vpass);
    bool forceNoMotion = any(unity_MotionVectorsParams.yw == 0.0);

    //Motion vector is enabled in SG but not active in VFX
#if defined(HAVE_VFX_MODIFICATION) && !VFX_FEATURE_MOTION_VECTORS
    forceNoMotion = true;
#endif

    // outMotionVec is already initialize at the value of forceNoMotion (see above)
    if (!forceNoMotion)
    {
        float2 motionVec = CalculateMotionVector(inputPass.positionCS, inputPass.previousPositionCS);
        EncodeMotionVector(motionVec * 0.5, outMotionVec);
        // Always write 1.0 in alpha since blend mode could be active on this target as a side effect of VT feedback buffer
        // motion vector expected output format is RG16
        outMotionVec.zw = 1.0;
    }
#endif

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif

#ifdef UNITY_VIRTUAL_TEXTURING
    outVTFeedback = PackVTFeedbackWithAlpha(builtinData.vtPackedFeedback, input.positionSS.xy, builtinData.opacity);
#endif
}
