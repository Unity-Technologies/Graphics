Shader "Hidden/Universal Render Pipeline/SkyPrerender"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE

        // TODO What are these for?
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings vert(Attributes input)
        {
            Varyings output;

            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?
            UNITY_TRANSFER_INSTANCE_ID(input, output); // TODO and this?

            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // TODO this too?

            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

            return output;
        }

        float frag(Varyings input) : SV_DEPTH
        {
            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // TODO What is this?

            // TODO Implement

            return 1;
        }

        ENDHLSL

        Pass
        {
            Name "SkyPrerender"
            ColorMask 0
            ZTest Always // TODO Change to greater
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
