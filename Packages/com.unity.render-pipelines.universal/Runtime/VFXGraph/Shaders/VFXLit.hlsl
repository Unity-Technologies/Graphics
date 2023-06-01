// Upgrade NOTE: replaced 'defined at' with 'defined (at)'
#ifndef SHADERPASS
#error SHADERPASS must be defined (at) this point
#endif

#if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SixWayLighting.hlsl"
    #include "Packages/com.unity.visualeffectgraph/Shaders/SixWay/VFXSixWayCommon.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#endif

float3 VFXGetPositionRWS(VFX_VARYING_PS_INPUTS i)
{
    float3 posWS = (float3)0;
    #ifdef VFX_VARYING_POSWS
    posWS = i.VFX_VARYING_POSWS;
    #endif
    return VFXGetPositionRWS(posWS);
}

InputData VFXGetInputData(const VFX_VARYING_PS_INPUTS i, const PositionInputs posInputs, float3 normalWS, bool frontFace)
{
    InputData inputData = (InputData)0;


    inputData.positionWS = posInputs.positionWS.xyz;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
#if defined(VFX_MATERIAL_TYPE_SIX_WAY_SMOKE) && defined(VFX_VARYING_TANGENT)
    float signNormal = frontFace ? 1.0 : -1.0f;
    float3 bitangent = cross(i.VFX_VARYING_NORMAL.xyz, i.VFX_VARYING_TANGENT.xyz);
    inputData.tangentToWorld = half3x3(i.VFX_VARYING_TANGENT.xyz, bitangent.xyz, signNormal * i.VFX_VARYING_NORMAL.xyz);
#endif

//When there is only one cascaded, this shadowCoord can be computed at vertex stage
//#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
//    inputData.shadowCoord = inputData.shadowCoord;
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    //This ComputeFogFactor can be moved to vertex and use interpolator instead
    float fogFactor = ComputeFogFactor(i.VFX_VARYING_POSCS.z);
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), fogFactor);

    //SampleSH could partially be done on vertex using SampleSHVertex & SampleSHPixel
    //For now, use directly the simpler per pixel fallback
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    inputData.bakedGI = SAMPLE_GI(0, // No vertex support
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        i.VFX_VARYING_POSCS.xy);
#else
    inputData.bakedGI = SampleSH(normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.VFX_VARYING_POSCS);

    //No static light map in VFX
    //inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    return inputData;
}


#ifndef VFX_SHADERGRAPH

float4 GetSurfaceColor(VFX_VARYING_PS_INPUTS i, const VFXUVData uvData)
{
    float4 baseColorMapSample = (float4)1.0f;
    float4 color = float4(1,1,1,1);
    #if URP_USE_BASE_COLOR
    color *= VFXGetParticleColor(i);
    #elif URP_USE_ADDITIONAL_BASE_COLOR
    #if defined(VFX_VARYING_COLOR)
    color.xyz *= i.VFX_VARYING_COLOR;
    #endif
    #if defined(VFX_VARYING_ALPHA)
    color.a *= i.VFX_VARYING_ALPHA;
    #endif
    #endif
    #if URP_USE_BASE_COLOR_MAP
    baseColorMapSample = SampleTexture(VFX_SAMPLER(baseColorMap),uvData);
    #if URP_USE_BASE_COLOR_MAP_COLOR
    color.xyz *= baseColorMapSample.xyz;
    #endif
    #if URP_USE_BASE_COLOR_MAP_ALPHA
    color.a *= baseColorMapSample.a;
    #endif
    #endif
    color = VFXApplySoftParticleFade(i, color);

    return color;
}

float3 GetSurfaceEmissive(VFX_VARYING_PS_INPUTS i, const VFXUVData uvData)
{
    float3 emission = float3(1, 1, 1);
    #if URP_USE_EMISSIVE_MAP
    float emissiveScale = 1.0f;
    #ifdef VFX_VARYING_EMISSIVESCALE
    emissiveScale = i.VFX_VARYING_EMISSIVESCALE;
    #endif
    emission *= SampleTexture(VFX_SAMPLER(emissiveMap), uvData).rgb * emissiveScale;
    #endif
    #if defined(VFX_VARYING_EMISSIVE) && (URP_USE_EMISSIVE_COLOR || URP_USE_ADDITIONAL_EMISSIVE_COLOR)
    emission *= i.VFX_VARYING_EMISSIVE;
    #endif
    #ifdef VFX_VARYING_EXPOSUREWEIGHT
    emission *= lerp(GetInverseCurrentExposureMultiplier(), 1.0f, i.VFX_VARYING_EXPOSUREWEIGHT);
    #endif
    return emission;
}

#if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
SixWaySurfaceData VFXGetSurfaceData(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData)
{
        SixWaySurfaceData surfaceData = (SixWaySurfaceData)0;

        float4 color = GetSurfaceColor(i, uvData);

        surfaceData.absorptionRange = 1.0f;
        #ifdef VFX_VARYING_ABSORPTIONRANGE
        surfaceData.absorptionRange = i.VFX_VARYING_ABSORPTIONRANGE;
        #endif
        float4 lightmapPositive = SampleTexture(VFX_SAMPLER(positiveAxesLightmap),uvData);
        float4 lightmapNegative = SampleTexture(VFX_SAMPLER(negativeAxesLightmap),uvData);
        surfaceData.rightTopBack = lightmapPositive.rgb;
        surfaceData.leftBottomFront = lightmapNegative.rgb;
        #if VFX_STRIPS_SWAP_UV
            SixWaySwapUV(surfaceData.rightTopBack, surfaceData.leftBottomFront);
        #endif
        float mapAlpha = lightmapPositive.a;
        color.a *= mapAlpha;
        #if VFX_SIX_WAY_REMAP
            #if VFX_BLENDMODE_PREMULTIPLY
                surfaceData.rightTopBack /= (mapAlpha + VFX_EPSILON);
                surfaceData.leftBottomFront /= (mapAlpha + VFX_EPSILON);
            #endif
            #if defined(VFX_VARYING_LIGHTMAP_REMAP_RANGES)
                float4 remapRanges = i.VFX_VARYING_LIGHTMAP_REMAP_RANGES;
                RemapLightMapsRangesFrom(surfaceData.rightTopBack, surfaceData.leftBottomFront, mapAlpha, remapRanges);
            #endif
            #if defined(VFX_VARYING_LIGHTMAP_REMAP_CONTROLS)
                float2 lightmapControls = i.VFX_VARYING_LIGHTMAP_REMAP_CONTROLS;
                RemapLightMaps(surfaceData.rightTopBack, surfaceData.leftBottomFront, lightmapControls);
            #elif defined(VFX_VARYING_LIGHTMAP_REMAP_CURVE)
                float4 remapCurve = i.VFX_VARYING_LIGHTMAP_REMAP_CURVE;
                RemapLightMaps(surfaceData.rightTopBack, surfaceData.leftBottomFront, remapCurve);
            #endif
            #if defined(VFX_VARYING_LIGHTMAP_REMAP_RANGES)
                RemapLightMapsRangesTo(surfaceData.rightTopBack, surfaceData.leftBottomFront, mapAlpha, remapRanges);
            #endif
            #if VFX_BLENDMODE_PREMULTIPLY
                surfaceData.rightTopBack *= (mapAlpha + VFX_EPSILON);
                surfaceData.leftBottomFront *= (mapAlpha + VFX_EPSILON);
            #endif
        #endif
        float invEnergy = INV_PI;
        surfaceData.rightTopBack *= invEnergy;
        surfaceData.leftBottomFront *= invEnergy;
        #if defined(VFX_VARYING_ALPHA_REMAP)
            color.a = SampleCurve(i.VFX_VARYING_ALPHA_REMAP, color.a);
        #endif
        VFXClipFragmentColor(color.a,i);

        #if defined(VFX_VARYING_BAKE_DIFFUSE_LIGHTING)
            surfaceData.diffuseGIData0 = i.VFX_VARYING_BAKE_DIFFUSE_LIGHTING[0];
            surfaceData.diffuseGIData1 = i.VFX_VARYING_BAKE_DIFFUSE_LIGHTING[1];
            surfaceData.diffuseGIData2 = i.VFX_VARYING_BAKE_DIFFUSE_LIGHTING[2];
        #endif

    #if URP_USE_EMISSIVE
    #if defined(VFX_VARYING_EMISSIVE_GRADIENT)
        float emissiveChannel = SampleTexture(VFX_SAMPLER(negativeAxesLightmap),uvData).a;
        #if defined(VFX_VARYING_EMISSIVE_CHANNEL_SCALE)
        emissiveChannel *= i.VFX_VARYING_EMISSIVE_CHANNEL_SCALE;
        #endif
        surfaceData.emission = SampleGradient(i.VFX_VARYING_EMISSIVE_GRADIENT, emissiveChannel).rgb;
        #if defined(VFX_VARYING_EMISSIVE_MULTIPLIER)
        surfaceData.emission *= i.VFX_VARYING_EMISSIVE_MULTIPLIER;
        #endif
        #ifdef VFX_VARYING_EXPOSUREWEIGHT
        surfaceData.emission *= lerp(GetInverseCurrentExposureMultiplier(),1.0f,i.VFX_VARYING_EXPOSUREWEIGHT);
        #endif
    #else
        surfaceData.emission = GetSurfaceEmissive(i, uvData);
    #endif
    surfaceData.emission *= saturate(color.a);
    #endif

    surfaceData.baseColor.rgb = color.rgb;
    surfaceData.alpha = saturate(color.a);
    surfaceData.occlusion = 1.0f;

    return surfaceData;

}
#else

SurfaceData VFXGetSurfaceData(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData)
{
    SurfaceData surfaceData = (SurfaceData)0;

    float4 color = GetSurfaceColor(i, uvData);

    VFXClipFragmentColor(color.a,i);
    surfaceData.albedo = saturate(color.rgb);

    #if IS_OPAQUE_PARTICLE
    surfaceData.alpha = 1.0f;
    #else
    surfaceData.alpha = saturate(color.a);
    #endif

    float4 metallicMapSample = (float4)1.0f;
    float4 specularMapSample = (float4)1.0f;
    #if URP_WORKFLOW_MODE_METALLIC
    surfaceData.metallic = 1.0f;
    #ifdef VFX_VARYING_METALLIC
    surfaceData.metallic *= i.VFX_VARYING_METALLIC;
    #endif
    #if URP_USE_METALLIC_MAP
    metallicMapSample = SampleTexture(VFX_SAMPLER(metallicMap), uvData);
    surfaceData.metallic *= metallicMapSample.r;
    #endif
    #elif URP_WORKFLOW_MODE_SPECULAR
    surfaceData.specular = (float3)1.0f;
    #ifdef VFX_VARYING_SPECULAR
    surfaceData.specular *= saturate(i.VFX_VARYING_SPECULAR);
    #endif
    #if URP_USE_SPECULAR_MAP
    specularMapSample = SampleTexture(VFX_SAMPLER(specularMap), uvData);
    surfaceData.specular *= specularMapSample.rgb;
    #endif
    #endif

    surfaceData.normalTS = float3(1.0f, 0.0f, 0.0f); //NormalWS is directly modified in VFX

    surfaceData.smoothness = 1.0f;
    #ifdef VFX_VARYING_SMOOTHNESS
    surfaceData.smoothness *= i.VFX_VARYING_SMOOTHNESS;
    #endif
    #if URP_USE_SMOOTHNESS_IN_ALBEDO
    surfaceData.smoothness *= SampleTexture(VFX_SAMPLER(baseColorMap),uvData).a;
    #elif URP_USE_SMOOTHNESS_IN_METALLIC
    surfaceData.smoothness *= metallicMapSample.a;
    #elif URP_USE_SMOOTHNESS_IN_SPECULAR
    surfaceData.smoothness *= specularMapSample.a;
    #endif

    surfaceData.occlusion = 1.0f;
    #if URP_USE_OCCLUSION_MAP
    float4 mask = SampleTexture(VFX_SAMPLER(occlusionMap),uvData);
    surfaceData.occlusion *= mask.g;
    #endif

    #if URP_USE_EMISSIVE
    surfaceData.emission = GetSurfaceEmissive(i, uvData);
    #endif

    return surfaceData;
}
#endif


#endif
