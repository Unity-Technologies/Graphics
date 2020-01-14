Shader "Hidden/Light2D-Global"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
            };

            half3 _LightColor;

            TEXTURE2D(_GBufferColor);
            SAMPLER(sampler_GBufferColor);

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                o.positionCS = float4(attributes.positionOS, 1.0f);
                o.uv = attributes.uv;
                o.uv.y = 1.0f - o.uv.y;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_GBufferColor, sampler_GBufferColor, i.uv);
                color.rgb *= _LightColor;
                return color;
            }
            ENDHLSL
        }
    }
}
