void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
{
    // setup defaults -- these are used if the graph doesn't output a value
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    // copy across graph values, if defined
    $SurfaceDescription.BaseColor: surfaceData.color = surfaceDescription.BaseColor;

    #ifdef WRITE_NORMAL_BUFFER
    // When we need to export the normal (in the depth prepass, we write the geometry one)
    surfaceData.normalWS = fragInputs.tangentToWorld[2];
    #endif

    #if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        // TODO
    }
    #endif

    #if defined(_ENABLE_SHADOW_MATTE) && SHADERPASS == SHADERPASS_FORWARD_UNLIT
        HDShadowContext shadowContext = InitShadowContext();
        float shadow;
        float3 shadow3;
        // We need to recompute some coordinate not computed by default for shadow matte
        posInput = GetPositionInput(fragInputs.positionSS.xy, _ScreenSize.zw, fragInputs.positionSS.z, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 upWS = normalize(fragInputs.tangentToWorld[1]);
        uint renderingLayers = GetMeshRenderingLightLayer();
        ShadowLoopMin(shadowContext, posInput, upWS, asuint(_ShadowMatteFilter), renderingLayers, shadow3);
        shadow = dot(shadow3, float3(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0));

        float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
        float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);

        // Keep the nested lerp
        // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
        // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
        #ifdef _SURFACE_TYPE_TRANSPARENT
            surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow), surfaceDescription.Alpha);
        #else
            surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow);
        #endif
        localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;

        surfaceDescription.Alpha = localAlpha;
    #endif
}
