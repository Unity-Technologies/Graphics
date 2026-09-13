#ifndef UNIVERSAL_SPEEDTREE8_INPUT_INCLUDED
#define UNIVERSAL_SPEEDTREE8_INPUT_INCLUDED

#ifdef EFFECT_BUMP
    #define _NORMALMAP 1
#endif

#define _ALPHATEST_ON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/NormalMap.hlsl"

#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    #define SPEEDTREE_Y_UP
    #define SPEEDTREE_8_WIND 1
    #include "SpeedTreeWind.cginc"
    UNITY_INSTANCING_BUFFER_START(STWind)
        UNITY_DEFINE_INSTANCED_PROP(float, _GlobalWindTime)
    UNITY_INSTANCING_BUFFER_END(STWind)
#endif

half4 _Color;
int _TwoSided;

#ifdef SCENESELECTIONPASS
    int _ObjectId;
    int _PassValue;
#endif

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);

half GetBumpScale()            { return half(1.0); }

half4 SampleBaseMap(float2 uv) { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv); }
half4 SampleBumpMap(float2 uv) { return SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv); }

#ifdef EFFECT_EXTRA_TEX
    sampler2D _ExtraTex;
#else
    half _Glossiness;
    half _Metallic;
#endif

#ifdef EFFECT_HUE_VARIATION
    half4 _HueVariationColor;
#endif

#ifdef EFFECT_BILLBOARD
    half _BillboardShadowFade;
#endif

#ifdef EFFECT_SUBSURFACE
    sampler2D _SubsurfaceTex;
    half4 _SubsurfaceColor;
    half _SubsurfaceIndirect;
#endif

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

#define GEOM_TYPE_BRANCH 0
#define GEOM_TYPE_FROND 1
#define GEOM_TYPE_LEAF 2
#define GEOM_TYPE_FACINGLEAF 3

#define _Surface 0.0 // Speed Trees are always opaque

UNITY_TEXTURE_STREAMING_DEBUG_VARS;

#endif
