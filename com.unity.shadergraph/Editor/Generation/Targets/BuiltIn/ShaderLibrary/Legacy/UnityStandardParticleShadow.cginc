#ifndef UNITY_STANDARD_PARTICLE_SHADOW_INCLUDED
#define UNITY_STANDARD_PARTICLE_SHADOW_INCLUDED

// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)

#if _REQUIRE_UV2
#define _FLIPBOOK_BLENDING 1
#endif

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardParticleInstancing.cginc"

#if (defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
    #define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#define UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef _ALPHATEST_ON
half        _Cutoff;
#endif
sampler2D   _MainTex;
float4      _MainTex_ST;
#ifdef UNITY_STANDARD_USE_DITHER_MASK
sampler3D   _DitherMaskLOD;
#endif

// Handle PremultipliedAlpha from Fade or Transparent shading mode
half        _Metallic;
#ifdef _METALLICGLOSSMAP
sampler2D   _MetallicGlossMap;
#endif

half MetallicSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half metallicity = _Metallic;
    #ifdef _METALLICGLOSSMAP
        metallicity = tex2D(_MetallicGlossMap, uv).r;
    #endif
    return OneMinusReflectivityFromMetallic(metallicity);
}

struct VertexInput
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    fixed4 color    : COLOR;
    #if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        float4 texcoords : TEXCOORD0;
        float texcoordBlend : TEXCOORD1;
    #else
        float2 texcoords : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
    V2F_SHADOW_CASTER_NOPOS
    #ifdef UNITY_STANDARD_USE_SHADOW_UVS
        float2 texcoord : TEXCOORD1;
        #ifdef _FLIPBOOK_BLENDING
            float3 texcoord2AndBlend : TEXCOORD2;
        #endif
        fixed4 color : TEXCOORD3;
    #endif
};
#endif

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster
{
    UNITY_VERTEX_OUTPUT_STEREO
};
#endif

// We have to do these dances of outputting SV_POSITION separately from the vertex shader,
// and inputting VPOS in the pixel shader, since they both map to "POSITION" semantic on
// some platforms, and then things don't go well.


void vertParticleShadowCaster (VertexInput v,
    #ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    out VertexOutputShadowCaster o,
    #endif
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
    out VertexOutputStereoShadowCaster os,
    #endif
    out float4 opos : SV_POSITION)
{
    UNITY_SETUP_INSTANCE_ID(v);
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
    #endif
    TRANSFER_SHADOW_CASTER_NOPOS(o,opos)
    #ifdef UNITY_STANDARD_USE_SHADOW_UVS
        #ifdef _FLIPBOOK_BLENDING
            #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
                vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
            #else
                o.texcoord = v.texcoords.xy;
                o.texcoord2AndBlend.xy = v.texcoords.zw;
                o.texcoord2AndBlend.z = v.texcoordBlend;
            #endif
        #else
            #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
                vertInstancingUVs(v.texcoords.xy, o.texcoord);
                o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
            #else
                o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
            #endif
        #endif
        o.color = v.color;
    #endif
}

half4 fragParticleShadowCaster (
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    VertexOutputShadowCaster i
#endif
#ifdef UNITY_STANDARD_USE_DITHER_MASK
    , UNITY_VPOS_TYPE vpos : VPOS
#endif
    ) : SV_Target
{
    #ifdef UNITY_STANDARD_USE_SHADOW_UVS
        half alpha = tex2D(_MainTex, i.texcoord).a;
        #ifdef _FLIPBOOK_BLENDING
            half alpha2 = tex2D(_MainTex, i.texcoord2AndBlend.xy).a;
            alpha = lerp(alpha, alpha2, i.texcoord2AndBlend.z);
        #endif
        alpha *= i.color.a;

        #ifdef _ALPHATEST_ON
            clip (alpha - _Cutoff);
        #endif
        #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
            #ifdef _ALPHAPREMULTIPLY_ON
                half outModifiedAlpha;
                PreMultiplyAlpha(half3(0, 0, 0), alpha, MetallicSetup_ShadowGetOneMinusReflectivity(i.texcoord), outModifiedAlpha);
                alpha = outModifiedAlpha;
            #endif
            #ifdef UNITY_STANDARD_USE_DITHER_MASK
                // Use dither mask for alpha blended shadows, based on pixel position xy
                // and alpha level. Our dither texture is 4x4x16.
                half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
                clip (alphaRef - 0.01);
            #else
                clip (alpha - 0.5);
            #endif
        #endif
    #endif // UNITY_STANDARD_USE_SHADOW_UVS)

    SHADOW_CASTER_FRAGMENT(i)
}

#endif // UNITY_STANDARD_PARTICLE_SHADOW_INCLUDED
