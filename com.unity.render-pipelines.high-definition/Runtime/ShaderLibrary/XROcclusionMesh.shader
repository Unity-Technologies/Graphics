Shader "Hidden/HDRP/XROcclusionMesh"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #pragma multi_compile _ XR_OCCLUSION_MESH_COMBINED

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        struct Attributes
        {
            float4 vertex : POSITION;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;

        #if XR_OCCLUSION_MESH_COMBINED
            uint rtArrayIndex : SV_RenderTargetArrayIndex;
        #endif
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = float4(input.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), UNITY_NEAR_CLIP_VALUE, 1.0f);
	    
        #if XR_OCCLUSION_MESH_COMBINED
            output.rtArrayIndex = input.vertex.z;
        #endif
	
            return output;
        }

        float4 _ClearColor;

        float4 Frag() : SV_Target
        {
            return _ClearColor;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
