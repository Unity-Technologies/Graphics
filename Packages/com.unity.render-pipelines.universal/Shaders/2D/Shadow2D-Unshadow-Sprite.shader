Shader "Hidden/Shadow2DUnshadowSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _ShadowColorMask("__ShadowColorMask", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Cull Off
        BlendOp Add
        Blend OneMinusSrcColor SrcColor, OneMinusSrcAlpha SrcAlpha
        ZWrite Off
        ZTest Always

        Pass
        {
            //Bit 0: Composite Shadow Bit, Bit 1: Global Shadow Bit
            Stencil
            {
                Ref  1
                Comp Equal
                Pass Keep
                Fail Keep
            }

            ColorMask [_ShadowColorMask]

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
            float4    _MainTex_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 main = tex2D(_MainTex, i.uv);
                half color = 1-main.a;
                return half4(color, color, color, color);
            }
            ENDHLSL
        }
    }
}
