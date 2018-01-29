// Shader targeted for low end devices. Single Pass Forward Rendering. Shader Model 2
Shader "LightweightPipeline/Standard (Simple Lighting)"
{
    // Keep properties of StandardSpecular shader for upgrade reasons.
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB) Glossiness / Alpha (A)", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Shininess("Shininess", Range(0.01, 1.0)) = 1.0
        _GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0

        _Glossiness("Glossiness", Range(0.0, 1.0)) = 0.5
        [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [HideInInspector] _SpecSource("Specular Color Source", Float) = 0.0
        _SpecColor("Specular", Color) = (1.0, 1.0, 1.0)
        _SpecGlossMap("Specular", 2D) = "white" {}
        [HideInInspector] _GlossinessSource("Glossiness Source", Float) = 0.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0

        // Blending state
        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" }
        LOD 300

        Pass
        {
            Tags { "LightMode" = "LightweightForward" }

            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 3.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
            #pragma shader_feature _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature _ _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION

            // -------------------------------------
            // Lightweight Pipeline keywords
            // We have no good approach exposed to skip shader variants, e.g, ideally we would like to skip _CASCADE for all puctual lights
            // Lightweight combines light classification and shadows keywords to reduce shader variants.
            // Lightweight shader library declares defines based on these keywords to avoid having to check them in the shaders
            // Core.hlsl defines _MAIN_LIGHT_DIRECTIONAL and _MAIN_LIGHT_SPOT (point lights can't be main light)
            // Shadow.hlsl defines _SHADOWS_ENABLED, _SHADOWS_SOFT, _SHADOWS_CASCADE, _SHADOWS_PERSPECTIVE
            #pragma multi_compile _ _MAIN_LIGHT_DIRECTIONAL_SHADOW _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE _MAIN_LIGHT_DIRECTIONAL_SHADOW_SOFT _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT _MAIN_LIGHT_SPOT_SHADOW _MAIN_LIGHT_SPOT_SHADOW_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_COOKIE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ FOG_LINEAR FOG_EXP2

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED LIGHTMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // LW doesn't support dynamic GI. So we save 30% shader variants if we assume
            // LIGHTMAP_ON when DIRLIGHTMAP_COMBINED is set
            #ifdef DIRLIGHTMAP_COMBINED
            #define LIGHTMAP_ON
            #endif

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragmentSimple
            #include "LWRP/ShaderLibrary/LightweightPassLit.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags{"Lightmode" = "ShadowCaster"}

            ZWrite On ZTest LEqual

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "LWRP/ShaderLibrary/LightweightPassShadow.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags{"Lightmode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "LWRP/ShaderLibrary/Core.hlsl"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(pos.xyz);
            }

            half4 frag() : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex LightweightVertexMeta
            #pragma fragment LightweightFragmentMetaSimple

            #pragma shader_feature _EMISSION
            #pragma shader_feature _SPECGLOSSMAP

            #include "LWRP/ShaderLibrary/LightweightPassMeta.hlsl"
            ENDHLSL
        }
    }
    Fallback "Hidden/InternalErrorShader"
    CustomEditor "LightweightStandardSimpleLightingGUI"
}
