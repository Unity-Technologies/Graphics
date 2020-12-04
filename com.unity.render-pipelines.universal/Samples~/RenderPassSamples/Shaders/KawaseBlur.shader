Shader "Custom/RenderFeature/KawaseBlur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;

            float _offset;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 res = _MainTex_TexelSize.xy;
                float i = _offset;

                half4 col;
                col.rgb = tex2D(_MainTex, input.uv).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(i, i) * res).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(i, -i) * res).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(-i, i) * res).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(-i, -i) * res).rgb;
                col.rgb /= 5.0f;

                return col;
            }
            ENDHLSL
        }
    }
}
