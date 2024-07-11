Shader "Hidden/Universal Render Pipeline/XR/XROcclusionMesh"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #pragma multi_compile _ XR_OCCLUSION_MESH_COMBINED

        // Not all platforms properly support SV_RenderTargetArrayIndex
        #if defined(SHADER_API_D3D11) || defined(SHADER_API_VULKAN) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_PSSL)
            #if defined (UNITY_STEREO_MULTIVIEW_ENABLED)
                #define USE_XR_OCCLUSION_MESH_COMBINED_MULTIVIEW XR_OCCLUSION_MESH_COMBINED
            #else
                #define USE_XR_OCCLUSION_MESH_COMBINED_RT_ARRAY_INDEX XR_OCCLUSION_MESH_COMBINED
            #endif
        #endif

        struct Attributes
        {
            float4 vertex : POSITION;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;

        #if USE_XR_OCCLUSION_MESH_COMBINED_RT_ARRAY_INDEX
            uint rtArrayIndex : SV_RenderTargetArrayIndex;
        #endif
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = mul(UNITY_MATRIX_M, float4(input.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), UNITY_NEAR_CLIP_VALUE, 1.0f));

        #if USE_XR_OCCLUSION_MESH_COMBINED_MULTIVIEW
            if (unity_StereoEyeIndex != uint(input.vertex.z))
            {
                output.vertex = float4(0.0f, 0.0f, 0.0f, 0.0f);
            }
        #elif USE_XR_OCCLUSION_MESH_COMBINED_RT_ARRAY_INDEX
            output.rtArrayIndex = input.vertex.z;
        #endif

            return output;
        }

        float4 Frag() : SV_Target
        {
            return (0.0f).xxxx;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite On ZTest LEqual Blend Off Cull Off
            ColorMask 0

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
