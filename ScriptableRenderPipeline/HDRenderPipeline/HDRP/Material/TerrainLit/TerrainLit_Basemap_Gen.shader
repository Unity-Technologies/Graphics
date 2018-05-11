Shader "Hidden/HDRenderPipeline/TerrainLit_Basemap_Gen"
{
    Properties
    {
        [HideInInspector] _Control0 ("AlphaMap", 2D) = "" {}
        [HideInInspector] _Control1 ("AlphaMap", 2D) = "" {}

        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat4 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat5 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat6 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat7 ("Layer 3 (A)", 2D) = "white" {}

        [HideInInspector] _Smoothness0 ("Smoothness0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness1 ("Smoothness1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness2 ("Smoothness2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness3 ("Smoothness3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness4 ("Smoothness0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness5 ("Smoothness1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness6 ("Smoothness2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness7 ("Smoothness3", Range(0.0, 1.0)) = 0.0

        [HideInInspector] _Metallic0 ("Metallic0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic1 ("Metallic1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic2 ("Metallic2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic3 ("Metallic3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic4 ("Metallic0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic5 ("Metallic1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic6 ("Metallic2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Metallic7 ("Metallic3", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags { "SplatCount" = "8" }

        HLSLINCLUDE

        #define USE_LEGACY_UNITY_MATRIX_VARIABLES
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../../ShaderVariables.hlsl"

        TEXTURE2D(_Control0);
        TEXTURE2D(_Control1);
        SAMPLER(sampler_Control0);

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

            TEXTURE2D(_Splat0);
            TEXTURE2D(_Splat1);
            TEXTURE2D(_Splat2);
            TEXTURE2D(_Splat3);
            TEXTURE2D(_Splat4);
            TEXTURE2D(_Splat5);
            TEXTURE2D(_Splat6);
            TEXTURE2D(_Splat7);
            SAMPLER(sampler_Splat0);

            float _Smoothness0;
            float _Smoothness1;
            float _Smoothness2;
            float _Smoothness3;
            float _Smoothness4;
            float _Smoothness5;
            float _Smoothness6;
            float _Smoothness7;

            float4 _Splat0_ST;
            float4 _Splat1_ST;
            float4 _Splat2_ST;
            float4 _Splat3_ST;
            float4 _Splat4_ST;
            float4 _Splat5_ST;
            float4 _Splat6_ST;
            float4 _Splat7_ST;

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
                float4 weights0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, i.texcoord);
                float4 weights1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, i.texcoord);

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

            float _Metallic0;
            float _Metallic1;
            float _Metallic2;
            float _Metallic3;
            float _Metallic4;
            float _Metallic5;
            float _Metallic6;
            float _Metallic7;

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
                float4 weights0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, i.texcoord);
                float4 weights1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, i.texcoord);

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
