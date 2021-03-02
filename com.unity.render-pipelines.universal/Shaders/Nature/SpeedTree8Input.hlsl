#ifndef UNIVERSAL_SPEEDTREE8_INPUT_INCLUDED
#define UNIVERSAL_SPEEDTREE8_INPUT_INCLUDED

#ifdef EFFECT_BUMP
    #define _NORMALMAP
#endif

#define _ALPHATEST_ON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    #define SPEEDTREE_Y_UP
    #include "SpeedTreeWind.cginc"
    float _WindEnabled;
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

#endif
