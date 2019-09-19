Shader "Hidden/Shadow2DRemoveSelf"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData][HideInInspector] _ShadowStencilGroup("__ShadowStencilGroup", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        BlendOp Add
        Blend One One
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref [_ShadowStencilGroup]
                Comp Equal
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
            float4 _MainTex_ST;
            float  _ReceivesShadows;

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
                col.r = 0;
                col.g = 0;
                col.b = main.a;
                col.a = 0;
                return col;
            }
            ENDHLSL
        }
    }
}
