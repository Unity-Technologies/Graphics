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
        BlendOp RevSub
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


            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

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
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 main = tex2D(_MainTex, i.uv);

                // sample the texture
                fixed4 col;
                col.r = 0;
                col.g = 0.5 * main.a;
                col.b = 0;
                col.a = 0;
                return col;
            }
            ENDCG
        }
    }
}
