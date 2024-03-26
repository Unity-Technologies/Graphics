Shader "Hidden/HDRP/VolumetricCloudsCombine"
{
    Properties {}

    SubShader
    {
        HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        //#pragma enable_d3d11_debug_symbols

        #pragma vertex Vert
        #pragma fragment Frag

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricClouds/VolumetricCloudsDef.cs.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

        // Aerial perspective is already applied during cloud tracing
        #define ATMOSPHERE_NO_AERIAL_PERSPECTIVE

        TEXTURE2D_X(_CameraColorTexture);
        TEXTURE2D_X(_VolumetricCloudsLightingTexture);
        TEXTURE2D_X(_VolumetricCloudsDepthTexture);
        TEXTURECUBE(_VolumetricCloudsTexture);
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
        ENDHLSL

        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            // Pass 0
            Cull   Off
            ZTest  Less // Required for XR occlusion mesh optimization
            ZWrite Off

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend 0 One OneMinusSrcAlpha, Zero One
            Blend 1 DstColor Zero // Multiply to combine the transmittance

            HLSLPROGRAM

            #pragma multi_compile_fragment _ OUTPUT_TRANSMITTANCE_BUFFER

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

            void Frag(Varyings input, out float4 color : SV_Target0
                #if defined(OUTPUT_TRANSMITTANCE_BUFFER)
                , out float2 fogTransmittance : SV_Target3
                #endif
                )
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Read cloud data
                color = LOAD_TEXTURE2D_X(_VolumetricCloudsLightingTexture, input.positionCS.xy);
                color.a = 1.0f - color.a;

                float transmittance = 1.0f - color.a;

                float deviceDepth = LOAD_TEXTURE2D_X(_VolumetricCloudsDepthTexture, input.positionCS.xy).x;
                float linearDepth = DecodeInfiniteDepth(deviceDepth, _CloudNearPlane);

                float3 V = GetSkyViewDirWS(input.positionCS.xy);
                float3 positionWS = GetCameraPositionWS() - linearDepth * V;

                // Compute pos inputs
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, positionWS);
                posInput.linearDepth = linearDepth;

                // Apply fog
                float3 volColor, volOpacity;
                EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);
                color.rgb = color.rgb * (1 - volOpacity) + volColor * color.a;

                // Output transmittance for lens flares
                #if defined(OUTPUT_TRANSMITTANCE_BUFFER)
                // channel 1 is used when fog multiple scattering is enabled and we don't want clouds in this opacity (it doesn't work well with water and transparent sorting)
                fogTransmittance = float2(transmittance, 1);
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 1
            Cull   Off
            ZWrite Off
            ZTest  Always
            Blend  Off

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                // Composite the result via hardware blending.
                float4 clouds = LOAD_TEXTURE2D_X(_VolumetricCloudsLightingTexture, input.positionCS.xy);
                clouds.rgb *= GetInverseCurrentExposureMultiplier();
                float4 color = LOAD_TEXTURE2D_X(_CameraColorTexture, input.positionCS.xy);
                return float4(clouds.xyz + color.xyz * clouds.w, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 2
            Cull   Off
            ZWrite Off
            ZTest  Always

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One SrcAlpha, Zero One

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                // Composite the result via hardware blending.
                float4 clouds = LOAD_TEXTURE2D_X(_VolumetricCloudsLightingTexture, input.positionCS.xy);
                clouds.rgb *= GetInverseCurrentExposureMultiplier();
                return clouds;
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
                return LOAD_TEXTURE2D_X(_VolumetricCloudsLightingTexture, input.positionCS.xy);
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 4
            Cull   Off
            ZWrite Off
            Blend  Off

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                // Points towards the camera
                float3 viewDirWS = -GetSkyViewDirWS(input.positionCS.xy * (float)_Mipmap);
                // Fetch the clouds
                return SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsTexture, s_linear_clamp_sampler, viewDirWS, _Mipmap);
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 5
            Cull   Off
            ZWrite Off
            Blend  Off

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                // Construct the view direction
                float3 viewDirWS = -GetSkyViewDirWS(input.positionCS.xy * (float)_Mipmap);
                // Fetch the clouds
                float4 clouds = SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsTexture, s_linear_clamp_sampler, viewDirWS, _Mipmap);
                // Inverse the exposure
                clouds.rgb *= GetInverseCurrentExposureMultiplier();
                // Read the color value
                float4 color = LOAD_TEXTURE2D_X(_CameraColorTexture, input.positionCS.xy);
                // Combine the clouds
                return float4(clouds.xyz + color.xyz * clouds.w, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 6
            Cull   Off
            ZWrite Off
            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One SrcAlpha, Zero One

            HLSLPROGRAM

            float4 Frag(Varyings input) : SV_Target
            {
                // Construct the view direction
                float3 viewDirWS = -GetSkyViewDirWS(input.positionCS.xy * (float)_Mipmap);
                // Fetch the clouds
                float4 clouds = SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsTexture, s_linear_clamp_sampler, viewDirWS, _Mipmap);
                // Inverse the exposure
                clouds.rgb *= GetInverseCurrentExposureMultiplier();
                return clouds;
            }
            ENDHLSL
        }

        Pass
        {
            // Pass 7
            // This pass does per pixel sorting with refractive objects
            // Mainly used to correctly sort clouds above water

            Cull   Off
            ZTest  Less // Required for XR occlusion mesh optimization
            ZWrite Off

            // If this is a background pixel, we want the cloud value, otherwise we do not.
            Blend  One OneMinusSrcAlpha, Zero One

            Blend 1 One OneMinusSrcAlpha // before refraction
            Blend 2 One OneMinusSrcAlpha // before refraction alpha
            Blend 3 DstColor Zero // Multiply to combine the transmittance

            HLSLPROGRAM

            #pragma multi_compile_fragment _ OUTPUT_TRANSMITTANCE_BUFFER

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialBlendModeEnum.cs.hlsl"
            #define _BlendMode BLENDINGMODE_ALPHA

            // For refraction sorting, clouds are considered pre-refraction transparents
            #define SUPPORT_WATER_ABSORPTION
            #define _TRANSPARENT_REFRACTIVE_SORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricClouds/VolumetricCloudsUtilities.hlsl"

            void Frag(Varyings input
                , out float4 color : SV_Target0
                , out float4 outBeforeRefractionColor : SV_Target1
                , out float4 outBeforeRefractionAlpha : SV_Target2
                #if defined(OUTPUT_TRANSMITTANCE_BUFFER)
                , out float2 fogTransmittance : SV_Target3
                #endif
            )
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Read cloud data
                color = LOAD_TEXTURE2D_X(_VolumetricCloudsLightingTexture, input.positionCS.xy);
                color.a = 1.0f - color.a;

                float transmittance = 1.0f - color.a;

                float deviceDepth = LOAD_TEXTURE2D_X(_VolumetricCloudsDepthTexture, input.positionCS.xy).x;
                float linearDepth = min(DecodeInfiniteDepth(deviceDepth, _CloudNearPlane), _ProjectionParams.z);

                float3 V = GetSkyViewDirWS(input.positionCS.xy);
                float3 positionWS = GetCameraPositionWS() - linearDepth * V;

                // Compute pos inputs
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, positionWS);
                posInput.linearDepth = linearDepth;
                posInput.deviceDepth = saturate(ConvertCloudDepth(positionWS));

                // Apply fog
                float3 volColor, volOpacity;
                EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);
                color.rgb = color.rgb * (1 - volOpacity) + volColor * color.a;

                // Sort clouds with refractive objects
                ComputeRefractionSplitColor(posInput, color, outBeforeRefractionColor, outBeforeRefractionAlpha);

                // Output transmittance for lens flares
                #if defined(OUTPUT_TRANSMITTANCE_BUFFER)
                // channel 1 is used when fog multiple scattering is enabled and we don't want clouds in this opacity (it doesn't work well with water and transparent sorting)
                fogTransmittance = float2(transmittance, 1);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
