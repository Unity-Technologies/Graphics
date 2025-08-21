// Upgrade NOTE: replaced 'defined at' with 'defined (at)'

#ifdef DEBUG_DISPLAY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif
#ifndef SHADERPASS
#error SHADERPASS must be defined (at) this point
#endif

#if defined(VFX_MATERIAL_TYPE_SIX_WAY_SMOKE)
#include "Packages/com.unity.visualeffectgraph/Shaders/SixWay/VFXSixWayCommon.hlsl"
#endif

#if defined(VFX_MATERIAL_TYPE_SIX_WAY_SMOKE) && (SHADERPASS == SHADERPASS_FORWARD)
//Do nothing. In Six-way lighting forward pass, these includes are required earlier, defined in VFXSixWayIncludes.template
#else
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

#if (SHADERPASS == SHADERPASS_FORWARD) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

    // The light loop (or lighting architecture) is in charge to:
    // - Define light list
    // - Define the light loop
    // - Setup the constant/data
    // - Do the reflection hierarchy
    // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

    #define HAS_LIGHTLOOP

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

    #ifdef HDRP_MATERIAL_TYPE_SIMPLE
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/SimpleLit.hlsl"
        #define _DISABLE_SSR
    #if defined(SHADER_STAGE_RAY_TRACING)
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/SimpleLitRayTracing.hlsl"
    #endif
    #else
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
    #if defined(SHADER_STAGE_RAY_TRACING)
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRayTracing.hlsl"
    #endif
    #endif

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

#else // (SHADERPASS == SHADERPASS_FORWARD) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
    #ifdef VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
        #include  "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SixWayLit/SixWaySmokeLit.hlsl"
    #else
        #ifdef HDRP_MATERIAL_TYPE_SIMPLE
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/SimpleLit.hlsl"
            #if defined(SHADER_STAGE_RAY_TRACING)
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/SimpleLitRayTracing.hlsl"
            #endif
        #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #if defined(SHADER_STAGE_RAY_TRACING)
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRayTracing.hlsl"
            #endif
        #endif
    #endif

#endif // (SHADERPASS == SHADERPASS_FORWARD)
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

float3 VFXGetPositionRWS(VFX_VARYING_PS_INPUTS i)
{
    float3 posWS = (float3)0;
    #ifdef VFX_VARYING_POSWS
    posWS = i.VFX_VARYING_POSWS;
    #endif
    return VFXGetPositionRWS(posWS);
}

BuiltinData VFXGetBuiltinData(const VFX_VARYING_PS_INPUTS i,const PositionInputs posInputs, const SurfaceData surfaceData, const VFXUVData uvData, float opacity = 1.0f)
{
    BuiltinData builtinData = (BuiltinData)0;

    InitBuiltinData(posInputs, opacity, surfaceData.normalWS, -surfaceData.normalWS, (float4)0, (float4)0, builtinData); // We dont care about uvs are we dont sample lightmaps

    #if HDRP_USE_EMISSIVE
    builtinData.emissiveColor = float3(1,1,1);
    #if HDRP_USE_EMISSIVE_MAP
    float emissiveScale = 1.0f;
    #ifdef VFX_VARYING_EMISSIVESCALE
    emissiveScale = i.VFX_VARYING_EMISSIVESCALE;
    #endif
    builtinData.emissiveColor *= SampleTexture(VFX_SAMPLER(emissiveMap),uvData).rgb * emissiveScale;
    #endif
    #if defined(VFX_VARYING_EMISSIVE) && (HDRP_USE_EMISSIVE_COLOR || HDRP_USE_ADDITIONAL_EMISSIVE_COLOR)
    builtinData.emissiveColor *= i.VFX_VARYING_EMISSIVE;
    #endif
    #ifdef VFX_VARYING_EXPOSUREWEIGHT
    builtinData.emissiveColor *= lerp(GetInverseCurrentExposureMultiplier(),1.0f,i.VFX_VARYING_EXPOSUREWEIGHT);
    #endif
    #endif

    #if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE && defined(VFX_VARYING_EMISSIVE_GRADIENT)
        float emissiveChannel = SampleTexture(VFX_SAMPLER(negativeAxesLightmap),uvData).a;
        #if defined(VFX_VARYING_EMISSIVE_CHANNEL_SCALE)
            emissiveChannel *= i.VFX_VARYING_EMISSIVE_CHANNEL_SCALE;
        #endif
        builtinData.emissiveColor = SampleGradient(i.VFX_VARYING_EMISSIVE_GRADIENT, emissiveChannel).rgb;
        #if defined(VFX_VARYING_EMISSIVE_MULTIPLIER)
            builtinData.emissiveColor *= i.VFX_VARYING_EMISSIVE_MULTIPLIER;
        #endif
        #ifdef VFX_VARYING_EXPOSUREWEIGHT
            builtinData.emissiveColor *= lerp(GetInverseCurrentExposureMultiplier(),1.0f,i.VFX_VARYING_EXPOSUREWEIGHT);
        #endif
    #endif

    builtinData.emissiveColor *= opacity;

    #if defined(SHADER_STAGE_RAY_TRACING)
    PostInitBuiltinData(-WorldRayDirection(),posInputs,surfaceData, builtinData);
    #else
    PostInitBuiltinData(GetWorldSpaceNormalizeViewDir(posInputs.positionWS),posInputs,surfaceData, builtinData);
    #endif
    return builtinData;
}


#ifndef VFX_SHADERGRAPH

SurfaceData VFXGetSurfaceData(const VFX_VARYING_PS_INPUTS i, float3 normalWS,const VFXUVData uvData, uint diffusionProfileHash, bool frontFace, out float opacity)
{
    SurfaceData surfaceData = (SurfaceData)0;

    float4 color = float4(1,1,1,1);
    #if HDRP_USE_BASE_COLOR
    color *= VFXGetParticleColor(i);
    #elif HDRP_USE_ADDITIONAL_BASE_COLOR
    #if defined(VFX_VARYING_COLOR)
    color.xyz *= i.VFX_VARYING_COLOR;
    #endif
    #if defined(VFX_VARYING_ALPHA)
    color.a *= i.VFX_VARYING_ALPHA;
    #endif
    #endif
    #if HDRP_USE_BASE_COLOR_MAP
    float4 colorMap = SampleTexture(VFX_SAMPLER(baseColorMap),uvData);
    #if HDRP_USE_BASE_COLOR_MAP_COLOR
    color.xyz *= colorMap.xyz;
    #endif
    #if HDRP_USE_BASE_COLOR_MAP_ALPHA
    color.a *= colorMap.a;
    #endif
    #endif

    #if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
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
        #if VFX_SIX_WAY_USE_ALPHA_REMAP
            color.a = SampleCurve(i.VFX_VARYING_ALPHA_REMAP, color.a);
        #endif

        float frontFaceSign = frontFace ? 1.0f : -1.0f;

        #if defined(VFX_VARYING_BAKE_DIFFUSE_LIGHTING)
            surfaceData.bakeDiffuseLighting0 = i.VFX_VARYING_BAKE_DIFFUSE_LIGHTING[0];
            surfaceData.bakeDiffuseLighting1 = i.VFX_VARYING_BAKE_DIFFUSE_LIGHTING[1];
            surfaceData.bakeDiffuseLighting2 = i.VFX_VARYING_BAKE_DIFFUSE_LIGHTING[2];
            surfaceData.bakeDiffuseLighting2.xyz *= frontFaceSign;
        #endif

        #if defined(VFX_VARYING_TANGENT)
        #if VFX_PRIMITIVE_QUAD || VFX_PRIMITIVE_TRIANGLE || VFX_PRIMITIVE_OCTAGON
        float4 tangentWS = float4(i.VFX_VARYING_TANGENT, -1.0f);
        #else
        float4 tangentWS = i.VFX_VARYING_TANGENT;
        #endif
        #else
        float4 tangentWS = float4(1,0,0,1);
        #endif

        surfaceData.tangentWS = tangentWS;
        surfaceData.bitangentWS = cross(normalWS, surfaceData.tangentWS.xyz) * frontFaceSign * tangentWS.w * GetOddNegativeScale();

        surfaceData.baseColor.a = mapAlpha;
    #endif

    color = VFXApplySoftParticleFade(i, color);
    #if !defined(SHADER_STAGE_RAY_TRACING)
    VFXClipFragmentColor(color.a, i);
    #endif

    surfaceData.baseColor.rgb = saturate(color.rgb);

    #if IS_OPAQUE_PARTICLE
    opacity = 1.0f;
    #else
    opacity = saturate(color.a);
    #endif

    #if HDRP_MATERIAL_TYPE_STANDARD
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
    #ifdef VFX_VARYING_METALLIC
    surfaceData.metallic = i.VFX_VARYING_METALLIC;
    #endif
    #elif HDRP_MATERIAL_TYPE_SPECULAR
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
    #ifdef VFX_VARYING_SPECULAR
    surfaceData.specularColor = saturate(i.VFX_VARYING_SPECULAR);
    #endif
    #elif HDRP_MATERIAL_TYPE_TRANSLUCENT
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
    #ifdef VFX_VARYING_THICKNESS
    surfaceData.thickness = i.VFX_VARYING_THICKNESS * opacity;
    #endif
    surfaceData.diffusionProfileHash = diffusionProfileHash;
    surfaceData.subsurfaceMask = 1.0f;
    surfaceData.transmissionMask = 1.0f;
    #endif

    surfaceData.normalWS = normalWS;
    surfaceData.ambientOcclusion = 1.0f;
    #ifndef VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
        #ifdef VFX_VARYING_SMOOTHNESS
        surfaceData.perceptualSmoothness = i.VFX_VARYING_SMOOTHNESS;
        #endif
        surfaceData.specularOcclusion = 1.0f;
    #endif


    #if HDRP_USE_MASK_MAP
    float4 mask = SampleTexture(VFX_SAMPLER(maskMap),uvData);
    surfaceData.metallic *= mask.r;
    surfaceData.ambientOcclusion *= mask.g;
    surfaceData.perceptualSmoothness *= mask.a;
    #endif

    return surfaceData;
}


#endif
