Shader "Hidden/Shadow2DUnshadowSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _ShadowColorMask("__ShadowColorMask", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }

        Pass
        {
            Stencil
            {
                Ref       1
                Comp      Always
                Pass      Replace
            }

            Cull Off
            Blend   One One
            BlendOp Add
            ZWrite  Off
            ZTest   Always

            ColorMask B 

            Name "Sprite Unshadow (B) - Stencil: Ref 1, Comp Always, Pass Replace"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct Varyings
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float     _ShadowAlphaCutoff;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = _Color.a * v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 main = i.color * tex2D(_MainTex, i.uv);

                if (main.a <= _ShadowAlphaCutoff)
                    discard;

                return half4(0, 0, main.a, 0);
            }
            ENDHLSL
        }
        // Remove stencil from previous stencil pass
        Pass
        {
            Stencil
            {
                Ref       0
                Comp      Always
                Pass      Replace
            }

            Blend   One One
            BlendOp Add
            Cull Off
            ZWrite Off
            ZTest Always

            ColorMask 0

            Name "Sprite Unshadow (B) - Stencil: Ref 0, Comp Always, Pass Replace"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct Varyings
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float     _ShadowAlphaCutoff;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = _Color.a * v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 main = i.color * tex2D(_MainTex, i.uv);

                if (main.a <= _ShadowAlphaCutoff)
                    discard;

                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}
