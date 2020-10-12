Shader "Hidden/Universal Render Pipeline/Sky/HDRISky"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE

        // TODO What are these for?
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Runtime/Sky/SkyUtils.hlsl"

        TEXTURECUBE(_Cubemap);
        SAMPLER(sampler_Cubemap);

        float4 _SkyParam; // x exposure, y multiplier, zw rotation (cosPhi and sinPhi)
        #define _Intensity          _SkyParam.x
        #define _CosSinPhi          _SkyParam.zw

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

        float3 RotationUp(float3 p, float2 cos_sin)
        {
            float3 rotDirX = float3(cos_sin.x, 0, -cos_sin.y);
            float3 rotDirY = float3(cos_sin.y, 0, cos_sin.x);

            return float3(dot(rotDirX, p), p.y, dot(rotDirY, p));
        }

        float3 GetSkyColor(float3 dir)
        {
            return SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgb;
        }

        float4 GetColorWithRotation(float3 dir, float exposure, float2 cos_sin)
        {
            dir = RotationUp(dir, cos_sin);

            float3 skyColor = GetSkyColor(dir) * _Intensity * exposure;
            skyColor = ClampToFloat16Max(skyColor);

            return float4(skyColor, 1.0);
        }

        float4 RenderSky(Varyings input, float exposure)
        {
            float3 viewDirWS = GetSkyViewDirWS(input.positionCS.xy);

            float3 dir = -viewDirWS; // Reverse it to point into the scene

            return GetColorWithRotation(dir, exposure, _CosSinPhi);
        }

        float4 FragRender(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input); // TODO What is this?

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // TODO What is this?

            return RenderSky(input, 1.0); // TODO CurrentExposureMultiplier
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
