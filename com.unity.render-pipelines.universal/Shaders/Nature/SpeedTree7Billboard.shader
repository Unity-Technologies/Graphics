Shader "Universal Render Pipeline/Nature/SpeedTree7 Billboard"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _HueVariation("Hue Variation", Color) = (1.0,0.5,0.0,0.1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.333
        [MaterialEnum(None,0,Fastest,1)] _WindQuality("Wind Quality", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "RenderType" = "TransparentCutout"
            "DisableBatching" = "LODFading"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
        }
        LOD 400

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex SpeedTree7Vert
            #pragma fragment SpeedTree7Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _CLUSTERED_RENDERING
            #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS
            #pragma multi_compile __ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_HUE_VARIATION

            #define ENABLE_WIND

            #include "SpeedTree7BillboardInput.hlsl"
            #include "SpeedTree7BillboardPasses.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS
            #pragma multi_compile __ LOD_FADE_CROSSFADE

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SHADOW_CASTER

            #include "SpeedTree7BillboardInput.hlsl"
            #include "SpeedTree7BillboardPasses.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            HLSLPROGRAM
            #pragma exclude_renderers gles
            #pragma vertex SpeedTree7Vert
            #pragma fragment SpeedTree7Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS
            #pragma multi_compile __ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_HUE_VARIATION

            #define ENABLE_WIND
            #define GBUFFER

            #include "SpeedTree7BillboardInput.hlsl"
            #include "SpeedTree7BillboardPasses.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS
            #pragma multi_compile __ LOD_FADE_CROSSFADE

            #define ENABLE_WIND
            #define DEPTH_ONLY

            #include "SpeedTree7BillboardInput.hlsl"
            #include "SpeedTree7BillboardPasses.hlsl"

            ENDHLSL
        }
    }
}
