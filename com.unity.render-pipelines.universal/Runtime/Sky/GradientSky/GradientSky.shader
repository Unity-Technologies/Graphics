Shader "Hidden/Universal Render Pipeline/SkyPrerender"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE

        // TODO What are these for?
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Runtime/Sky/SkyUtils.hlsl"

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_Position;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;

            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?
            UNITY_TRANSFER_INSTANCE_ID(input, output); // TODO and this?

            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // TODO this too?

            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);

            return output;
        }

        float FragPrerender(Varyings input) : SV_Depth
        {
            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // TODO What is this?

            // TODO Implement
            // TODO Actually, remove this, prerender isn't needed by this sky type

            return 1;
        }

        float4 RenderSky(Varyings input)
        {
            float4 _GradientBottom = float4(1, 0, 0, 1);
            float4 _GradientMiddle = float4(0, 1, 0, 1);
            float4 _GradientTop = float4(0, 0, 1, 1);

            float3 viewDirWS = GetSkyViewDirWS(input.positionCS.xy);

            float verticalGradient = viewDirWS.y; // TODO Gradient diffusion
            float topLerpFactor = saturate(-verticalGradient);
            float bottomLerpFactor = saturate(verticalGradient);

            float3 color = lerp(lerp(_GradientMiddle.xyz, _GradientBottom.xyz, bottomLerpFactor), _GradientTop.xyz, topLerpFactor);
            // TODO Sky intensity and exposure

            return float4(color, 1);
        }

        float4 FragRender(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // TODO What is this?

            return RenderSky(input);
        }

        ENDHLSL

        Pass
        {
            Name "SkyPrerender"
            Cull Off
            ZTest Always // TODO Change to greater
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPrerender
            ENDHLSL
        }

        Pass
        {
            Name "SkyRender"
            Cull Off
            ZTest LEqual
            ZWrite Off
            Blend Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragRender
            ENDHLSL
        }
    }
}
