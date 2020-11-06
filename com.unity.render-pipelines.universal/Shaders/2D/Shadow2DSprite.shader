Shader "Hidden/Shadow2DRemoveSelf"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData][HideInInspector] _SelfShadowing("__SelfShadowing", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        BlendOp Add
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            // Bit 0: Group Bit, Bit 1: Shadow Bit
            Stencil
            {
                Ref  1
                Comp LEqual
                Pass Keep
                Fail Keep
            }

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
            float     _SelfShadowing;   // This should be either 0 or 1

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

                half4 col;
                col.r = _SelfShadowing;
                col.g = _SelfShadowing;
                col.b = _SelfShadowing;
                col.a = main.a;
                return col;
            }
            ENDHLSL
        }
    }
}
