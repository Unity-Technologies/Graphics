Shader "ScriptableRenderPipeline/LightweightPipeline/Standard (PBS)"
{
    Properties
    {
        // Specular vs Metallic workflow
        [HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [Toggle] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [Toggle] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
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
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 300

        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Tags{"LightMode" = "LightweightForward"}

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------
            #pragma shader_feature _METALLIC_SETUP _SPECULAR_SETUP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ _METALLICSPECGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _ _OCCLUSIONMAP
            #pragma shader_feature _PARALLAXMAP

            #pragma multi_compile _ _SINGLE_DIRECTIONAL_LIGHT _SINGLE_SPOT_LIGHT _SINGLE_POINT_LIGHT
            #pragma multi_compile _ _LIGHTWEIGHT_FORCE_LINEAR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _HARD_SHADOWS _SOFT_SHADOWS _HARD_SHADOWS_CASCADES _SOFT_SHADOWS_CASCADES
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile _ _ATTENUATION_TEXTURE

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "UnityCG.cginc"
            #include "UnityStandardInput.cginc"
            #include "LightweightPassLit.cginc"
            ENDCG
        }

        Pass
        {
            Tags{"Lightmode" = "ShadowCaster"}

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "UnityCG.cginc"
            #include "LightweightPassShadow.cginc"
            ENDCG
        }

        Pass
        {
            Tags{"Lightmode" = "DepthOnly"}

            ZWrite On

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(pos);
            }

            half4 frag() : SV_TARGET
            {
                return 0;
            }
            ENDCG
        }
    }
    FallBack "Standard"
    CustomEditor "LightweightStandardShaderGUI"
}

