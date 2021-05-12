Shader "Hidden/Shadow2DUnshadowGeometry"
{
    Properties
    {
        [HideInInspector] _ShadowColorMask("__ShadowColorMask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            //Bit 0: Composite Shadow Bit, Bit 1: Global Shadow Bit
            Stencil
            {
                Ref  1
                Comp Equal
                Pass DecrSat
                Fail Keep
            }

            ColorMask[_ShadowColorMask]

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


            Varyings vert (Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}
