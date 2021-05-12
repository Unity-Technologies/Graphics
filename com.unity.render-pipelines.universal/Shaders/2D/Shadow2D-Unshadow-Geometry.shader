Shader "Hidden/Shadow2DUnshadowGeometry"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            //Bit 0: Composite Shadow Bit, Bit 1: Global Shadow Bit, Bit2: Caster Mask Bit
            Stencil
            {
                Ref  4
                ReadMask  4
                WriteMask 4
                Comp NotEqual
                Pass Replace
                Fail Keep
            }

            ColorMask 0

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

        Pass
        {
            //Bit 0: Composite Shadow Bit, Bit 1: Global Shadow Bit, Bit2: Caster Mask Bit
            Stencil
            {
                Ref  4
                ReadMask  4
                WriteMask 4
                Comp Equal
                Pass Zero
                Fail Keep
            }

            ColorMask 0

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
                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}
