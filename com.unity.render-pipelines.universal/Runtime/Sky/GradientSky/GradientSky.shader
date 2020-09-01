Shader "Hidden/Universal Render Pipeline/Sky/GradientSky"
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

        float4 _GradientBottom;
        float4 _GradientMiddle;
        float4 _GradientTop;
        float _GradientDiffusion;
        float _SkyIntensity;

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

        float4 RenderSky(Varyings input)
        {
            float3 viewDirWS = GetSkyViewDirWS(input.positionCS.xy);

            float verticalGradient = viewDirWS.y * _GradientDiffusion;
            float topLerpFactor = saturate(-verticalGradient);
            float bottomLerpFactor = saturate(verticalGradient);

            float3 color = lerp(lerp(_GradientMiddle.xyz, _GradientBottom.xyz, bottomLerpFactor), _GradientTop.xyz, topLerpFactor);

            return float4(color * _SkyIntensity, 1);
        }

        float4 FragRender(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // TODO What is this?

            return RenderSky(input); // TODO CurentExposureMultiplier
        }

        ENDHLSL

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
