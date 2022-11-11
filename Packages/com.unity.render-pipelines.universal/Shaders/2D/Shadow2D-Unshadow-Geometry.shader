Shader "Hidden/Shadow2DUnshadowGeometry"
{
    Properties
    {
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
            Blend   SrcColor Zero
            BlendOp Add
            ZWrite Off
            ZTest Always

            ColorMask 0

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

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Stencil
            {
                Ref       0
                Comp      Always
                Pass      Replace
            }

            Cull Off
            Blend   One One
            BlendOp Add
            ZWrite Off
            ZTest Always

            ColorMask B

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}
