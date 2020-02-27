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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

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

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                o.positionCS = float4(attributes.positionOS, 1.0f);
                o.uv = attributes.uv;
                o.uv.y = 1.0f - o.uv.y;
                o.uv *= _GBufferColor_TexelSize.zw;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return BlendLightingWithBaseColor(_LightColor, i.uv);
            }
            ENDHLSL
        }
    }
}
