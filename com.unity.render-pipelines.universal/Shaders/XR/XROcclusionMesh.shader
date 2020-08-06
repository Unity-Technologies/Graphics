Shader "Hidden/Universal Render Pipeline/XR/XROcclusionMesh"
{
    HLSLINCLUDE
        #pragma exclude_renderers d3d11_9x
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 vertex : POSITION;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = mul(UNITY_MATRIX_M, input.vertex);
            return output;
        }

        void Frag(out float outputDepth : SV_Depth)
        {
            outputDepth = UNITY_NEAR_CLIP_VALUE;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off
            ColorMask 0

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
