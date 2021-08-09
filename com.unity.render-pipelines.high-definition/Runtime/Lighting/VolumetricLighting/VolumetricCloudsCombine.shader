Shader "Hidden/HDRP/VolumetricCloudsCombine"
{
    Properties {}

    SubShader
    {
        HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #pragma vertex Vert
        #pragma fragment Frag

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VolumetricCloudsDef.cs.hlsl"

        TEXTURE2D_X(_VolumetricCloudsUpscaleTextureRW);
        TEXTURECUBE(_VolumetricCloudsTexture);
        float4x4 _PixelCoordToViewDirWS;
        int _Mipmap;

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

        float3 UnapplyFastTonemapping(float3 input)
        {
            if (_EnableFastToneMapping)
                return input*rcp(1.0 - input);
            else
                return input;
        }
        ENDHLSL

        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            // Pass 0
            Cull   Off
            ZTest  Less // Required for XR occlusion mesh optimization
            ZWrite Off

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One SrcAlpha, Zero One

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Composite the result via hardware blending.
                return LOAD_TEXTURE2D_X(_VolumetricCloudsUpscaleTextureRW, input.positionCS.xy);
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 1
            Cull   Off
            ZWrite Off

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One SrcAlpha, Zero One

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                // Composite the result via hardware blending.
                float4 color = LOAD_TEXTURE2D_X(_VolumetricCloudsUpscaleTextureRW, input.positionCS.xy);
                // Reverse the tone mapping and exposure
                color.rgb = UnapplyFastTonemapping(color.rgb) * GetInverseCurrentExposureMultiplier();
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 2
            Cull   Off
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            float4 Frag(Varyings input) : SV_Target
            {
                return LOAD_TEXTURE2D_X(_VolumetricCloudsUpscaleTextureRW, input.positionCS.xy);
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 3
            Cull   Off
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            float4 Frag(Varyings input) : SV_Target
            {
                // Points towards the camera
                float3 viewDirWS = -normalize(mul(float4(input.positionCS.xy * (float)_Mipmap, 1.0, 1.0), _PixelCoordToViewDirWS));
                float4 val = SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsTexture, s_trilinear_clamp_sampler, viewDirWS, _Mipmap);
                return val;
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 4
            Cull   Off
            ZWrite Off

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One SrcAlpha, Zero One

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = -normalize(mul(float4(input.positionCS.xy * (float)_Mipmap, 1.0, 1.0), _PixelCoordToViewDirWS));
                float4 color = SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsTexture, s_trilinear_clamp_sampler, viewDirWS, _Mipmap);
                color.rgb = UnapplyFastTonemapping(color.rgb) * GetInverseCurrentExposureMultiplier();
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
