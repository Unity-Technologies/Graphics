Shader "Unlit/SimpleSRPUnlit"
{
    Properties
    {
        _SRPColor("Color", Color) = (1,1,1,1)
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  }
        LOD 100

        Pass
        {
//        Tags{"LightMode" = "Always"}
        Tags{"LightMode" = "Universal Forward"}


        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_cbuffer

            #include "HLSLSupport.cginc"

            CBUFFER_START(UnityPerMaterial)
                    float4 _SRPColor;
            CBUFFER_END

            CBUFFER_START(UnityPerDraw)
                float4x4 unity_ObjectToWorld;
                float4x4 unity_WorldToObject;
                float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
                float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms
            CBUFFER_END

            CBUFFER_START(UnityPerCamera)
                float4x4 unity_MatrixVP;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
                o.uv = v.uv * 4.0f + 0.50f;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                col *= _SRPColor;
                // apply fog
                return col;
            }
            ENDHLSL
        }
    }
}
