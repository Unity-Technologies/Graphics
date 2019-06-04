Shader "Lightweight Render Pipeline/Nature/SpeedTree7"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _HueVariation("Hue Variation", Color) = (1.0,0.5,0.0,0.1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _DetailTex("Detail", 2D) = "black" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.333
        [MaterialEnum(Off,0,Front,1,Back,2)] _Cull("Cull", Int) = 2
        [MaterialEnum(None,0,Fastest,1,Fast,2,Better,3,Best,4,Palm,5)] _WindQuality("Wind Quality", Range(0,5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
            "DisableBatching" = "LODFading"
            "RenderPipeline" = "LightweightPipeline"
        }
        LOD 400
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "LightweightForward" }

            HLSLPROGRAM

            #pragma vertex SpeedTree7Vert
            #pragma fragment SpeedTree7Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile_fog

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH
            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_HUE_VARIATION

            #define ENABLE_WIND
            #define VERTEX_COLOR

            #include "SpeedTree7Input.hlsl"
            #include "SpeedTree7Passes.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags{"LightMode" = "SceneSelectionPass"}

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SCENESELECTIONPASS

            #include "SpeedTree7Input.hlsl"
            #include "SpeedTree7Passes.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            HLSLPROGRAM

            #pragma vertex SpeedTree7VertDepth
            #pragma fragment SpeedTree7FragDepth

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH

            #define ENABLE_WIND
            #define DEPTH_ONLY
            #define SHADOW_CASTER

            #include "SpeedTree7Input.hlsl"
            #include "SpeedTree7Passes.hlsl"
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

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:50
            
            #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH

            #define ENABLE_WIND
            #define DEPTH_ONLY

            #include "SpeedTree7Input.hlsl"
            #include "SpeedTree7Passes.hlsl"

            ENDHLSL
        }
    }

    Dependency "BillboardShader" = "Lightweight Render Pipeline/Nature/SpeedTree7 Billboard"
    CustomEditor "SpeedTreeMaterialInspector"
}
