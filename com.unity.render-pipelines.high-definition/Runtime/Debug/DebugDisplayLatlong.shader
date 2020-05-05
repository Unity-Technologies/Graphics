Shader "Hidden/HDRP/DebugDisplayLatlong"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURECUBE_ARRAY(_InputCubemap);
            SAMPLER(sampler_InputCubemap);
            float _Mipmap;
            float _SliceIndex;
            float _ApplyExposure;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                uint width, height, depth, mipCount;
                width = height = depth = mipCount = 0;
                _InputCubemap.GetDimensions(0, width, height, depth, mipCount);
                mipCount = clamp(mipCount, 0, UNITY_SPECCUBE_LOD_STEPS);

                float3 skyColor = SAMPLE_TEXTURECUBE_ARRAY_LOD(_InputCubemap, sampler_InputCubemap, LatlongToDirectionCoordinate(input.texcoord.xy), _SliceIndex, _Mipmap * mipCount).rgb;

                return float4(skyColor * (_ApplyExposure > 0.0 ? GetCurrentExposureMultiplier() : 1.0), 1.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
