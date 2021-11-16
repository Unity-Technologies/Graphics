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

    #ifdef _ENABLE_SHADOW_MATTE

        #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)

            HDShadowContext shadowContext = InitShadowContext();

            // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
            float3 shadow3;
            ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);

            // Compute the average value in the fourth channel
            float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));

            float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
            float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);

            // Keep the nested lerp
            // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
            // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
            #ifdef _SURFACE_TYPE_TRANSPARENT
                surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
            #else
                surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
            #endif
            localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;

            surfaceDescription.Alpha = localAlpha;

        #elif SHADERPASS == SHADERPASS_PATH_TRACING

            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;

        #endif

    #endif // _ENABLE_SHADOW_MATTE
}
