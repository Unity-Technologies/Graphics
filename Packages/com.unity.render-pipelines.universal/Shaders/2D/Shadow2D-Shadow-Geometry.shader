Shader "Hidden/Shadow2DShadowGeometry"
{
    Properties
    {
        [HideInInspector] _ShadowColorMask("__ShadowColorMask", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        BlendOp Add
        Blend One One
        ZWrite Off
        ZTest Always

        // Process the shadow
        Pass
        {
            ColorMask R

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
                return half4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}
