#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
#error SHADERPASS_is_not_correctly_define
#endif

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

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadowLoop.hlsl"

float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, uint2(input.positionSS.xy)/GetTileSize());

    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    HDShadowContext shadowContext = InitShadowContext();
    float shadow;
    float3 shadow3;
    //posInput.positionWS = packedInput.vmesh.interpolators0;
    float3 normalWS = normalize(packedInput.vmesh.interpolators1);
    // Use uniform directly - The float need to be cast to uint (as unity don't support to set a uint as uniform)
    uint renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;
    ShadowLoopMin(shadowContext, posInput, normalWS, _ShadowFilter, renderingLayers, shadow3);

    shadow = dot(shadow3, float3(1.0f/3.0f, 1.0f/3.0f, 1.0f/3.0f));

    float2 shadowMatteColorMapUv = TRANSFORM_TEX(input.texCoord0.xy, _ShadowTintMap);

    float4 shadowTint = SAMPLE_TEXTURE2D(_ShadowTintMap, sampler_ShadowTintMap, shadowMatteColorMapUv).rgba*_ShadowTint.rgba;

    float4 outColor;

    float4 shadowColor = (1 - shadow)*shadowTint.rgba;
    float  localAlpha  = saturate(shadowColor.a + builtinData.opacity);

    // Keep the nested lerp
    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
#ifdef _SURFACE_TYPE_TRANSPARENT
    outColor.rgb = lerp(shadowColor.rgb*bsdfData.color.rgb, lerp(lerp(shadowColor.rgb, bsdfData.color.rgb, 1 - shadowTint.a), bsdfData.color.rgb, shadow), builtinData.opacity);
#else
    outColor.rgb = lerp(lerp(shadowColor.rgb, bsdfData.color.rgb, 1 - shadowTint.a), bsdfData.color.rgb, shadow);
#endif
    outColor = ApplyBlendMode(outColor.rgb, localAlpha);
    outColor = EvaluateAtmosphericScattering(posInput, V, outColor);

#ifdef DEBUG_DISPLAY
    // Same code in ShaderPassForward.shader
    // Reminder: _DebugViewMaterialArray[i]
    //   i==0 -> the size used in the buffer
    //   i>0  -> the index used (0 value means nothing)
    // The index stored in this buffer could either be
    //   - a gBufferIndex (always stored in _DebugViewMaterialArray[1] as only one supported)
    //   - a property index which is different for each kind of material even if reflecting the same thing (see MaterialSharedProperty)
    int bufferSize = int(_DebugViewMaterialArray[0]);
    // Loop through the whole buffer
    // Works because GetSurfaceDataDebug will do nothing if the index is not a known one
    for (int index = 1; index <= bufferSize; index++)
    {
        int indexMaterialProperty = int(_DebugViewMaterialArray[index]);
        if (indexMaterialProperty != 0)
        {
            float3 result = float3(1.0, 0.0, 1.0);
            bool needLinearToSRGB = false;

            GetPropertiesDataDebug(indexMaterialProperty, result, needLinearToSRGB);
            GetVaryingsDataDebug(indexMaterialProperty, input, result, needLinearToSRGB);
            GetBuiltinDataDebug(indexMaterialProperty, builtinData, result, needLinearToSRGB);
            GetSurfaceDataDebug(indexMaterialProperty, surfaceData, result, needLinearToSRGB);
            GetBSDFDataDebug(indexMaterialProperty, bsdfData, result, needLinearToSRGB);
            
            // TEMP!
            // For now, the final blit in the backbuffer performs an sRGB write
            // So in the meantime we apply the inverse transform to linear data to compensate.
            if (!needLinearToSRGB)
                result = SRGBToLinear(max(0, result));

            outColor = float4(result, 1.0);
        }
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
    {
        float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
        outColor = result;
    }
#endif

    return outColor;
}
