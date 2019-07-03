Shader "Hidden/HDRP/GUITextureBlit2SRGB" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MipLevel("_MipLevel", Range(0.0,7.0)) = 0.0
        _Exposure("_Exposure", Range(-10.0,10.0)) = 0.0
        _Color("_Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma editor_sync_compilation

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _MipLevel;
                float _Exposure;
                float4 _Color;
                float4 _MainTex_ST;
                float _ManualTex2SRGB;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                ZERO_INITIALIZE(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 color = SAMPLE_TEXTURE2D_X_LOD(_MainTex, s_trilinear_clamp_sampler, i.uv, _MipLevel);
                color = color * exp2(_Exposure) * GetCurrentExposureMultiplier();
                return color * _Color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
