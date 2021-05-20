Shader "Universal Render Pipeline/Nature/SpeedTree8"
{
    Properties
    {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Toggle(EFFECT_HUE_VARIATION)] _HueVariationKwToggle("Hue Variation", Float) = 0
        _HueVariationColor ("Hue Variation Color", Color) = (1.0,0.5,0.0,0.1)

        [Toggle(EFFECT_BUMP)] _NormalMapKwToggle("Normal Mapping", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _ExtraTex ("Smoothness (R), Metallic (G), AO (B)", 2D) = "(0.5, 0.0, 1.0)" {}
        _Glossiness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0

        [Toggle(EFFECT_SUBSURFACE)] _SubsurfaceKwToggle("Subsurface", Float) = 0
        _SubsurfaceTex ("Subsurface (RGB)", 2D) = "white" {}
        _SubsurfaceColor ("Subsurface Color", Color) = (1,1,1,1)
        _SubsurfaceIndirect ("Subsurface Indirect", Range(0.0, 1.0)) = 0.25

        [Toggle(EFFECT_BILLBOARD)] _BillboardKwToggle("Billboard", Float) = 0
        _BillboardShadowFade ("Billboard Shadow Fade", Range(0.0, 1.0)) = 0.5

        [Enum(No,2,Yes,0)] _TwoSided ("Two Sided", Int) = 2 // enum matches cull mode
        [KeywordEnum(None,Fastest,Fast,Better,Best,Palm)] _WindQuality ("Wind Quality", Range(0,5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "DisableBatching"="LODFading"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 400
        Cull [_TwoSided]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex SpeedTree8Vert
            #pragma fragment SpeedTree8Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile __ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
            #pragma shader_feature_local EFFECT_BILLBOARD
            #pragma shader_feature_local EFFECT_HUE_VARIATION
            #pragma shader_feature_local EFFECT_SUBSURFACE // GI dependent.
            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_EXTRA_TEX

            #define ENABLE_WIND
            #define EFFECT_BACKSIDE_NORMALS

            #include "SpeedTree8Input.hlsl"
            #include "SpeedTree8Passes.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags{"LightMode" = "SceneSelectionPass"}

            HLSLPROGRAM

            #pragma vertex SpeedTree8VertDepth
            #pragma fragment SpeedTree8FragDepth

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile __ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
            #pragma shader_feature_local EFFECT_BILLBOARD

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SCENESELECTIONPASS

            #include "SpeedTree8Input.hlsl"
            #include "SpeedTree8Passes.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree8VertDepth
            #pragma fragment SpeedTree8FragDepth

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile __ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
            #pragma shader_feature_local EFFECT_BILLBOARD

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SHADOW_CASTER

            #include "SpeedTree8Input.hlsl"
            #include "SpeedTree8Passes.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree8VertDepth
            #pragma fragment SpeedTree8FragDepth

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile __ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
            #pragma shader_feature_local EFFECT_BILLBOARD

            #define ENABLE_WIND
            #define DEPTH_ONLY

            #include "SpeedTree8Input.hlsl"
            #include "SpeedTree8Passes.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "SpeedTree8ShaderGUI"
}
