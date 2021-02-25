Shader "Hidden/HDRP/CombineLighting"
{
    Properties
    {
        [HideInInspector] _StencilMask("_StencilMask", Int) = 7
        [HideInInspector] _StencilRef("_StencilRef", Int) = 1
    }

    SubShader
    {
        HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        // #pragma enable_d3d11_debug_symbols

        #pragma vertex Vert
        #pragma fragment Frag

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D_X(_IrradianceSource);

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_Position;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }
        ENDHLSL

        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Less	   // Required for XR occlusion mesh optimization
            ZWrite Off
            Blend  One One // Additive

            HLSLPROGRAM
            
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return LOAD_TEXTURE2D_X(_IrradianceSource, input.positionCS.xy);
            }
            ENDHLSL
        }

        Pass
        {
            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Less    // Required for XR occlusion mesh optimization
            ZWrite Off
            Blend  One One // Additive

            HLSLPROGRAM
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return LOAD_TEXTURE2D_X(_IrradianceSource, input.positionCS.xy) * GetCurrentExposureMultiplier();
            }
            ENDHLSL
        }
    }
    Fallback Off
}
