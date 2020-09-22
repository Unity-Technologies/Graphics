Shader "Hidden/Universal Render Pipeline/XR/XROcclusionMesh"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 vertex : POSITION;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;

        #if defined(XR_OCCLUSION_MESH_COMBINED)
            uint rtArrayIndex : SV_RenderTargetArrayIndex;
        #endif
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = float4(input.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), UNITY_NEAR_CLIP_VALUE, 1.0f);

        #if defined(XR_OCCLUSION_MESH_COMBINED)
            output.rtArrayIndex = input.vertex.z;
        #endif

            return output;
        }

        float4 Frag() : SV_Target
        {
            return (0.0f).xxxx;
        }

    ENDHLSL

    // Not all platforms properly support SV_RenderTargetArrayIndex
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off
            ColorMask 0

            HLSLPROGRAM
                #pragma exclude_renderers gles metal
                #pragma vertex Vert
                #pragma fragment Frag
                #pragma multi_compile _ XR_OCCLUSION_MESH_COMBINED
            ENDHLSL
        }
    }

    // Fallback for platforms that do not support SV_RenderTargetArrayIndex (metal OSX supports it, metal iOS does not).
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off
            ColorMask 0

            HLSLPROGRAM
                #pragma only_renderers gles metal
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }

    Fallback Off
}
