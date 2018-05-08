Shader "Hidden/HDRenderPipeline/TerrainLit_Basemap_Gen"
{
    Properties
    {
        [HideInInspector] _Control ("AlphaMap", 2D) = "" {}

        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}

        [HideInInspector] _Smoothness0("Smoothness0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1("Smoothness1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2("Smoothness2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3("Smoothness3", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _Metallic0("Metallic0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Metallic1("Metallic1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Metallic2("Metallic2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Metallic3("Metallic3", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }
    SubShader
    {
        HLSLINCLUDE

        #define USE_LEGACY_UNITY_MATRIX_VARIABLES
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../../ShaderVariables.hlsl"

        TEXTURE2D(_Control);
        SAMPLER(sampler_Control);

        struct appdata_t {
            float3 vertex : POSITION;
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
                "EmptyColor" = "FFFFFFFF"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            TEXTURE2D(_Splat0);
            TEXTURE2D(_Splat1);
            TEXTURE2D(_Splat2);
            TEXTURE2D(_Splat3);
            SAMPLER(sampler_Splat0);

            float _Smoothness0;
            float _Smoothness1;
            float _Smoothness2;
            float _Smoothness3;

            float4 _Splat0_ST;
            float4 _Splat1_ST;
            float4 _Splat2_ST;
            float4 _Splat3_ST;

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                float2 texcoord3 : TEXCOORD3;
                float2 texcoord4 : TEXCOORD4;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                float3 positionWS = TransformObjectToWorld(v.vertex);
                o.vertex = TransformWorldToHClip(positionWS);
                o.texcoord0 = v.texcoord;
                o.texcoord1 = v.texcoord * _Splat0_ST.xy + _Splat0_ST.zw;
                o.texcoord2 = v.texcoord * _Splat1_ST.xy + _Splat1_ST.zw;
                o.texcoord3 = v.texcoord * _Splat2_ST.xy + _Splat2_ST.zw;
                o.texcoord4 = v.texcoord * _Splat3_ST.xy + _Splat3_ST.zw;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 weights = SAMPLE_TEXTURE2D(_Control, sampler_Control, i.texcoord0);
                float4 splat0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, i.texcoord1);
                float4 splat1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, i.texcoord2);
                float4 splat2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, i.texcoord3);
                float4 splat3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, i.texcoord4);

                splat0.a *= _Smoothness0;
                splat1.a *= _Smoothness1;
                splat2.a *= _Smoothness2;
                splat3.a *= _Smoothness3;

                float4 albedoSmoothness = splat0 * weights.x;
                albedoSmoothness += splat1 * weights.y;
                albedoSmoothness += splat2 * weights.z;
                albedoSmoothness += splat3 * weights.w;
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
                "EmptyColor" = "00000000"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            float _Metallic0;
            float _Metallic1;
            float _Metallic2;
            float _Metallic3;

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                float3 positionWS = TransformObjectToWorld(v.vertex);
                o.vertex = TransformWorldToHClip(positionWS);
                o.texcoord0 = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 weights = SAMPLE_TEXTURE2D(_Control, sampler_Control, i.texcoord0);

                float4 metallic = { _Metallic0 * weights.x, 0, 0, 0 };
                metallic.r += _Metallic1 * weights.y;
                metallic.r += _Metallic2 * weights.z;
                metallic.r += _Metallic3 * weights.w;
                return metallic;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
