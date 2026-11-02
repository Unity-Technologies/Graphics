#ifndef UNIVERSAL_LIGHTMAPS_INCLUDED
#define UNIVERSAL_LIGHTMAPS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Fallbacks so the lightmap keywords always evaluate as values (if (LIGHTMAP_ON))
// even in passes that don't declare them.

#if !defined(LIGHTMAP_ON_KEYWORD_DECLARED)
    #if !defined(LIGHTMAP_ON)
        static const bool LIGHTMAP_ON = 0;
    #elif DEFINED_NONZERO(LIGHTMAP_ON)
        #undef LIGHTMAP_ON
        #define LIGHTMAP_ON 1
    #endif
#endif

#if !defined(DYNAMICLIGHTMAP_ON_KEYWORD_DECLARED)
    #if !defined(DYNAMICLIGHTMAP_ON)
        static const bool DYNAMICLIGHTMAP_ON = 0;
    #elif DEFINED_NONZERO(DYNAMICLIGHTMAP_ON)
        #undef DYNAMICLIGHTMAP_ON
        #define DYNAMICLIGHTMAP_ON 1
    #endif
#endif

#if !defined(DIRLIGHTMAP_COMBINED_KEYWORD_DECLARED)
    #if !defined(DIRLIGHTMAP_COMBINED)
        static const bool DIRLIGHTMAP_COMBINED = 0;
    #elif DEFINED_NONZERO(DIRLIGHTMAP_COMBINED)
        #undef DIRLIGHTMAP_COMBINED
        #define DIRLIGHTMAP_COMBINED 1
    #endif
#endif

// Interpolator allocation flags, always defined as 0 or 1: whether the Varyings carry the
// lightmap UV / vertex SH / dynamic lightmap UV field. Pre-defining a deprecated REQUIRES_*
// alias still forces the field on for existing callers.
#if defined(LIGHTMAP_ON_KEYWORD_DECLARED) && (LIGHTMAP_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #define USE_LIGHTMAP_UV_INTERPOLATOR 1
    #define USE_VERTEX_SH_INTERPOLATOR 1
#elif !defined(LIGHTMAP_ON_KEYWORD_DECLARED) || (LIGHTMAP_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(LIGHTMAP_ON)
        #define USE_LIGHTMAP_UV_INTERPOLATOR 1
    #else
        #define USE_VERTEX_SH_INTERPOLATOR 1
    #endif
#endif

#if defined(DYNAMICLIGHTMAP_ON_KEYWORD_DECLARED) && (DYNAMICLIGHTMAP_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #define USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR 1
#elif !defined(DYNAMICLIGHTMAP_ON_KEYWORD_DECLARED) || (DYNAMICLIGHTMAP_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(DYNAMICLIGHTMAP_ON)
        #define USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR 1
    #endif
#endif

#if !defined(USE_LIGHTMAP_UV_INTERPOLATOR)
    #if defined(REQUIRES_LIGHTMAP_UV_INTERPOLATOR)
        #define USE_LIGHTMAP_UV_INTERPOLATOR 1
    #else
        #define USE_LIGHTMAP_UV_INTERPOLATOR 0
    #endif
#endif
#if !defined(USE_VERTEX_SH_INTERPOLATOR)
    #if defined(REQUIRES_VERTEX_SH_INTERPOLATOR)
        #define USE_VERTEX_SH_INTERPOLATOR 1
    #else
        #define USE_VERTEX_SH_INTERPOLATOR 0
    #endif
#endif
#if !defined(USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR)
    #if defined(REQUIRES_DYNAMICLIGHTMAP_UV_INTERPOLATOR)
        #define USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR 1
    #else
        #define USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR 0
    #endif
#endif

// Deprecated aliases for existing callers; new code reads the USE_* flags.
#if USE_LIGHTMAP_UV_INTERPOLATOR && !defined(REQUIRES_LIGHTMAP_UV_INTERPOLATOR)
    #define REQUIRES_LIGHTMAP_UV_INTERPOLATOR 1
#endif
#if USE_VERTEX_SH_INTERPOLATOR && !defined(REQUIRES_VERTEX_SH_INTERPOLATOR)
    #define REQUIRES_VERTEX_SH_INTERPOLATOR 1
#endif
#if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR && !defined(REQUIRES_DYNAMICLIGHTMAP_UV_INTERPOLATOR)
    #define REQUIRES_DYNAMICLIGHTMAP_UV_INTERPOLATOR 1
#endif

// Runtime query functions — work with both specialized variants and dynamic branching.

bool LightmapAvailable()
{
    return LIGHTMAP_ON;
}

bool DynamicLightmapAvailable()
{
    return DYNAMICLIGHTMAP_ON;
}

bool DirectionalLightmapAvailable()
{
    return DIRLIGHTMAP_COMBINED;
}

float2 TransformLightmapUV(float2 lightmapUV, float4 lightmapScaleOffset)
{
    return lightmapUV * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
}

// Lightmap texture binding macros — resolve DOTS instancing texture arrays vs individual textures.
#if defined(UNITY_DOTS_INSTANCING_ENABLED) && !defined(USE_LEGACY_LIGHTMAPS)
#define LIGHTMAP_NAME unity_Lightmaps
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV, unity_LightmapIndex.x
#else
#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV
#endif

// Sample baked and/or realtime lightmap. Non-Direction and Directional if available.
half3 SampleLightmap(float2 staticLightmapUV, float2 dynamicLightmapUV, half3 normalWS)
{
    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, universal pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

    float3 diffuseLighting = 0;

    if (LightmapAvailable())
    {
        if (DirectionalLightmapAvailable())
        {
            diffuseLighting = SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
                TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
                LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, normalWS, true);
        }
        else
        {
            diffuseLighting = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
                LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, true);
        }
    }

    // unity_DynamicLightmap is always a Texture2D, but in the DOTS texture-array path
    // SampleDirectional/SingleLightmap are Texture2DArray-typed. Dynamic lightmaps are not
    // supported with GPU-driven rendering, so skip them when the array path is active.
#if !defined(UNITY_DOTS_INSTANCING_ENABLED) || defined(USE_LEGACY_LIGHTMAPS)
    if (DynamicLightmapAvailable())
    {
        if (DirectionalLightmapAvailable())
        {
            diffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
                TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
                dynamicLightmapUV, transformCoords, normalWS, false);
        }
        else
        {
            diffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
                dynamicLightmapUV, transformCoords, false);
        }
    }
#endif

    return diffuseLighting;
}

// Legacy version of SampleLightmap where Realtime GI is not supported.
half3 SampleLightmap(float2 staticLightmapUV, half3 normalWS)
{
    float2 dummyDynamicLightmapUV = float2(0,0);
    return SampleLightmap(staticLightmapUV, dummyDynamicLightmapUV, normalWS);
}

// Deprecated macros, kept for shaders that still use them: use the USE_* interpolator flags
// for struct layout, TransformLightmapUV()/SampleProbeSHVertex() for vertex output, and
// InitializeBakedGI() for fragment input.
#if USE_LIGHTMAP_UV_INTERPOLATOR && USE_VERTEX_SH_INTERPOLATOR
    // Deprecated: use the USE_* interpolator flags instead.
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, _unused) float2 lmName : LIGHTMAPUV; half3 shName : VERTEXSH
    // Deprecated: use TransformLightmapUV() instead.
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = LightmapAvailable() ? TransformLightmapUV(lightmapUV.xy, lightmapScaleOffset) : float2(0, 0);
    // Deprecated: use SampleProbeSHVertex() instead.
    #ifdef USE_APV_PROBE_OCCLUSION
        #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) if (!LightmapAvailable()) { OUT.xyz = SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir, OUT_OCCLUSION); } else { OUT.xyz = half3(0, 0, 0); }
    #else
        #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) if (!LightmapAvailable()) { OUT.xyz = SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir); } else { OUT.xyz = half3(0, 0, 0); }
    #endif
    // Deprecated: use SampleSHVertex() instead.
    #define OUTPUT_SH(normalWS, OUT) OUT.xyz = !LightmapAvailable() ? SampleSHVertex(normalWS) : half3(0, 0, 0)
#elif REQUIRES_LIGHTMAP_UV_INTERPOLATOR
    // Deprecated: use REQUIRES_LIGHTMAP_UV_INTERPOLATOR instead.
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, _unused) float2 lmName : LIGHTMAPUV
    // Deprecated: use TransformLightmapUV() instead.
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = TransformLightmapUV(lightmapUV.xy, lightmapScaleOffset);
    #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION)
    #define OUTPUT_SH(normalWS, OUT)
#else
    // Deprecated: use REQUIRES_VERTEX_SH_INTERPOLATOR instead.
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, _unused) half3 shName : VERTEXSH
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
    // Deprecated: use SampleProbeSHVertex() instead.
    #ifdef USE_APV_PROBE_OCCLUSION
        #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) OUT.xyz = SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir, OUT_OCCLUSION)
    #else
        #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) OUT.xyz = SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir)
    #endif
    // Deprecated: use SampleSHVertex() instead.
    #define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
#endif

#endif // UNIVERSAL_LIGHTMAPS_INCLUDED
