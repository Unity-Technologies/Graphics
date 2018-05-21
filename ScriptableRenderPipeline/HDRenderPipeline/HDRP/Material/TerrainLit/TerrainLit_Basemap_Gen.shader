Shader "Hidden/HDRenderPipeline/TerrainLit_Basemap_Gen"
{
    Properties
    {
        [HideInInspector] _Control0("AlphaMap", 2D) = "" {}
        [HideInInspector] _Control1("AlphaMap", 2D) = "" {}

        [HideInInspector] _Splat0("Layer 0", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2", 2D) = "white" {}
        [HideInInspector] _Splat3("Layer 3", 2D) = "white" {}
        [HideInInspector] _Splat4("Layer 4", 2D) = "white" {}
        [HideInInspector] _Splat5("Layer 5", 2D) = "white" {}
        [HideInInspector] _Splat6("Layer 6", 2D) = "white" {}
        [HideInInspector] _Splat7("Layer 7", 2D) = "white" {}

        [HideInInspector] _Height0("Height 0", 2D) = "black" {}
        [HideInInspector] _Height1("Height 1", 2D) = "black" {}
        [HideInInspector] _Height2("Height 2", 2D) = "black" {}
        [HideInInspector] _Height3("Height 3", 2D) = "black" {}
        [HideInInspector] _Height4("Height 4", 2D) = "black" {}
        [HideInInspector] _Height5("Height 5", 2D) = "black" {}
        [HideInInspector] _Height6("Height 6", 2D) = "black" {}
        [HideInInspector] _Height7("Height 7", 2D) = "black" {}

        [HideInInspector] _HeightAmplitude0("Height Scale0", Float) = 0.02
        [HideInInspector] _HeightAmplitude1("Height Scale1", Float) = 0.02
        [HideInInspector] _HeightAmplitude2("Height Scale2", Float) = 0.02
        [HideInInspector] _HeightAmplitude3("Height Scale3", Float) = 0.02
        [HideInInspector] _HeightAmplitude4("Height Scale4", Float) = 0.02
        [HideInInspector] _HeightAmplitude5("Height Scale5", Float) = 0.02
        [HideInInspector] _HeightAmplitude6("Height Scale6", Float) = 0.02
        [HideInInspector] _HeightAmplitude7("Height Scale7", Float) = 0.02
        [HideInInspector] _HeightCenter0("Height Bias0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter1("Height Bias1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter2("Height Bias2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter3("Height Bias3", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter4("Height Bias4", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter5("Height Bias5", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter6("Height Bias6", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter7("Height Bias7", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness4("Smoothness 4", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness5("Smoothness 5", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness6("Smoothness 6", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness7("Smoothness 7", Range(0.0, 1.0)) = 0.0

        [HideInInspector] [Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic4("Metallic 4", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic5("Metallic 5", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic6("Metallic 6", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic7("Metallic 7", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags { "SplatCount" = "8" }

        HLSLINCLUDE

        #define USE_LEGACY_UNITY_MATRIX_VARIABLES
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../../ShaderVariables.hlsl"
        #define _TERRAIN_8_SPLATS
        #include "TerrainLitSplatCommon.hlsl"

        #pragma shader_feature _TERRAIN_HEIGHT_MAP
        // Needed because unity tries to match the name of the used textures to samplers. Heights can be used without splats.
        SAMPLER(sampler_Height0);

        void FetchWeights(float2 uv, out float4 weights0, out float4 weights1)
        {
            weights0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, uv);
            weights1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, uv);

            #ifdef _TERRAIN_HEIGHT_MAP
                float4 weightedHeights0;
                float4 weightedHeights1;
                weightedHeights0.r = (SAMPLE_TEXTURE2D(_Height0, sampler_Height0, TRANSFORM_TEX(uv, _Splat0)).r * weights0.r - _HeightCenter0) * _HeightAmplitude0;
                weightedHeights0.g = (SAMPLE_TEXTURE2D(_Height1, sampler_Height0, TRANSFORM_TEX(uv, _Splat1)).r * weights0.g - _HeightCenter1) * _HeightAmplitude1;
                weightedHeights0.b = (SAMPLE_TEXTURE2D(_Height2, sampler_Height0, TRANSFORM_TEX(uv, _Splat2)).r * weights0.b - _HeightCenter2) * _HeightAmplitude2;
                weightedHeights0.a = (SAMPLE_TEXTURE2D(_Height3, sampler_Height0, TRANSFORM_TEX(uv, _Splat3)).r * weights0.a - _HeightCenter3) * _HeightAmplitude3;
                weightedHeights1.r = (SAMPLE_TEXTURE2D(_Height4, sampler_Height0, TRANSFORM_TEX(uv, _Splat4)).r * weights1.r - _HeightCenter4) * _HeightAmplitude4;
                weightedHeights1.g = (SAMPLE_TEXTURE2D(_Height5, sampler_Height0, TRANSFORM_TEX(uv, _Splat5)).r * weights1.g - _HeightCenter5) * _HeightAmplitude5;
                weightedHeights1.b = (SAMPLE_TEXTURE2D(_Height6, sampler_Height0, TRANSFORM_TEX(uv, _Splat6)).r * weights1.b - _HeightCenter6) * _HeightAmplitude6;
                weightedHeights1.a = (SAMPLE_TEXTURE2D(_Height7, sampler_Height0, TRANSFORM_TEX(uv, _Splat7)).r * weights1.a - _HeightCenter7) * _HeightAmplitude7;

                // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
                ApplyHeightBlend(weightedHeights0, weightedHeights1, weights0, weights1);
            #endif
        }

        struct appdata_t {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        ENDHLSL

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appdata_t v)
            {
                v2f o;
                float3 positionWS = TransformObjectToWorld(v.vertex);
                o.vertex = TransformWorldToHClip(positionWS);
                o.texcoord = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 weights0, weights1;
                FetchWeights(i.texcoord, weights0, weights1);

                float4 splat0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat0));
                float4 splat1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat1));
                float4 splat2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat2));
                float4 splat3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat3));
                float4 splat4 = SAMPLE_TEXTURE2D(_Splat4, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat4));
                float4 splat5 = SAMPLE_TEXTURE2D(_Splat5, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat5));
                float4 splat6 = SAMPLE_TEXTURE2D(_Splat6, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat6));
                float4 splat7 = SAMPLE_TEXTURE2D(_Splat7, sampler_Splat0, TRANSFORM_TEX(i.texcoord, _Splat7));

                splat0.a *= _Smoothness0;
                splat1.a *= _Smoothness1;
                splat2.a *= _Smoothness2;
                splat3.a *= _Smoothness3;
                splat4.a *= _Smoothness4;
                splat5.a *= _Smoothness5;
                splat6.a *= _Smoothness6;
                splat7.a *= _Smoothness7;

                float4 albedoSmoothness = splat0 * weights0.x;
                albedoSmoothness += splat1 * weights0.y;
                albedoSmoothness += splat2 * weights0.z;
                albedoSmoothness += splat3 * weights0.w;
                albedoSmoothness += splat4 * weights1.x;
                albedoSmoothness += splat5 * weights1.y;
                albedoSmoothness += splat6 * weights1.z;
                albedoSmoothness += splat7 * weights1.w;
                return albedoSmoothness;
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "R8"
                "Size" = "1/4"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appdata_t v)
            {
                v2f o;
                float3 positionWS = TransformObjectToWorld(v.vertex);
                o.vertex = TransformWorldToHClip(positionWS);
                o.texcoord = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 weights0, weights1;
                FetchWeights(i.texcoord, weights0, weights1);

                float4 metallic = { _Metallic0 * weights0.x, 0, 0, 0 };
                metallic.r += _Metallic1 * weights0.y;
                metallic.r += _Metallic2 * weights0.z;
                metallic.r += _Metallic3 * weights0.w;
                metallic.r += _Metallic4 * weights1.x;
                metallic.r += _Metallic5 * weights1.y;
                metallic.r += _Metallic6 * weights1.z;
                metallic.r += _Metallic7 * weights1.w;
                return metallic;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
