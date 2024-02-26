Shader "Hidden/HDRP/OpaqueAtmosphericScattering"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        // #pragma enable_d3d11_debug_symbols

        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ OUTPUT_TRANSMITTANCE_BUFFER

        #if defined(SUPPORT_WATER) || defined(SUPPORT_WATER_CAUSTICS) || defined(SUPPORT_WATER_CAUSTICS_SHADOW)
        #define SUPPORT_WATER_ABSORPTION
        #endif
        #ifdef SUPPORT_WATER_CAUSTICS_SHADOW
        #define SUPPORT_WATER_CAUSTICS
        #endif

        #define OPAQUE_FOG_PASS

        // Defined for caustics
        #define SHADOW_LOW
        #define AREA_SHADOW_LOW

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

        TEXTURE2D_X_MSAA(float4, _ColorTextureMS);
        TEXTURE2D_X_MSAA(float,  _DepthTextureMS);
        TEXTURE2D_X(_ColorTexture);
        float _MultipleScatteringIntensity;

        // Water absorption stuff
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl"

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct FragOutput
        {
            float4 color : SV_Target0;
#if defined(OUTPUT_TRANSMITTANCE_BUFFER)
            float2 opticalFogTransmittance : SV_Target1;
#endif
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        void AtmosphericScatteringCompute(float2 positionCS, float3 V, float depth, bool atmosphericScattering, out float3 color, out float3 opacity, out float3 opacityWithoutWater)
        {
            PositionInputs posInput = GetPositionInput(positionCS, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

            #ifdef SUPPORT_WATER_ABSORPTION
            if (_EnableWater == 0 || !EvaluateUnderwaterAbsorption(posInput, color, opacity))
            {
            #endif

            if (depth == UNITY_RAW_FAR_CLIP_VALUE)
            {
                // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
                // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
                // And recompute the position on the sphere with the current camera direction.
                posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;

                // Warning: we do not modify depth values. Use them with care!
            }

            // Warning: this is a hack to strip the atmospheric scattering code for some variants
            // and we can't have different #define for each pass (we need multicompile but it generates variants)
            if (!atmosphericScattering)
                posInput.deviceDepth = UNITY_RAW_FAR_CLIP_VALUE;

            EvaluateAtmosphericScattering(posInput, V, color, opacity); // Premultiplied alpha
            opacityWithoutWater = opacity;

            #ifdef SUPPORT_WATER_ABSORPTION
            }
            else
            {
                opacityWithoutWater = 0;
            }
            #endif
        }

        FragOutput ComputeFragmentOutput(float4 outputColor, float3 opacity)
        {
            FragOutput output;

            output.color = outputColor;
#if defined(OUTPUT_TRANSMITTANCE_BUFFER)
            float finalOpacity = (opacity.x + opacity.y + opacity.z) / 3.0f;
            output.opticalFogTransmittance = 1 - finalOpacity;
#endif
            return output;
        }

        FragOutput Frag(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS  = input.positionCS.xy;
            float3 V           = GetSkyViewDirWS(positionSS);
            float  depth       = LoadCameraDepth(positionSS);

            float3 volColor, volOpacity, opacity2;
            AtmosphericScatteringCompute(positionSS, V, depth, false, volColor, volOpacity, opacity2);

            return ComputeFragmentOutput(float4(volColor, 1.0 - volOpacity.x), volOpacity);
        }

        FragOutput FragMSAA(Varyings input, uint sampleIndex: SV_SampleIndex)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS  = input.positionCS.xy;
            float3 V           = GetSkyViewDirWS(positionSS);
            float  depth       = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, (int2)positionSS, sampleIndex).x;

            float3 volColor, volOpacity, opacity2;
            AtmosphericScatteringCompute(positionSS, V, depth, false, volColor, volOpacity, opacity2);

            return ComputeFragmentOutput(float4(volColor, 1.0 - volOpacity.x), volOpacity);
        }

        FragOutput FragPolychromatic(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS = input.positionCS.xy;
            float3 V = GetSkyViewDirWS(positionSS);
            float  depth = LoadCameraDepth(positionSS);
            float4 surfColor = LOAD_TEXTURE2D_X(_ColorTexture, (int2)positionSS);

            float3 volColor, volOpacity, opacity2;
            AtmosphericScatteringCompute(positionSS, V, depth, true, volColor, volOpacity, opacity2);

            return ComputeFragmentOutput(float4(volColor + (1 - volOpacity) * surfColor.rgb, surfColor.a), volOpacity); // Premultiplied alpha (over operator), preserve alpha for the alpha channel for compositing
        }

        FragOutput FragMSAAPolychromatic(Varyings input, uint sampleIndex: SV_SampleIndex)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS = input.positionCS.xy;
            float3 V = GetSkyViewDirWS(positionSS);
            float  depth = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, (int2)positionSS, sampleIndex).x;
            float4 surfColor = LOAD_TEXTURE2D_X_MSAA(_ColorTextureMS, (int2)positionSS, sampleIndex);

            float3 volColor, volOpacity, opacity2;
            AtmosphericScatteringCompute(positionSS, V, depth, true, volColor, volOpacity, opacity2);

            return ComputeFragmentOutput(float4(volColor + (1 - volOpacity) * surfColor.rgb, surfColor.a), volOpacity); // Premultiplied alpha (over operator), preserve alpha for the alpha channel for compositing
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: NOMSAA
        Pass
        {
            Name "Default"

            Cull Off    ZWrite Off
            Blend 0 One SrcAlpha, Zero One // Premultiplied alpha for RGB, preserve alpha for the alpha channel
            Blend 1 Off
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }

        // 1: MSAA
        Pass
        {
            Name "MSAA"

            Cull Off    ZWrite Off
            Blend 0 One SrcAlpha, Zero One // Premultiplied alpha for RGB, preserve alpha for the alpha channel
            Blend 1 Off
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragMSAA
            ENDHLSL
        }

        // 2: NOMSAA Polychromatic Alpha
        Pass
        {
            Name "Polychromatic Alpha"

            Cull Off    ZWrite Off
            Blend Off   // Manual blending
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma multi_compile_fragment NO_WATER SUPPORT_WATER SUPPORT_WATER_CAUSTICS SUPPORT_WATER_CAUSTICS_SHADOW
                #pragma vertex Vert
                #pragma fragment FragPolychromatic
            ENDHLSL
        }

        // 3: MSAA Polychromatic Alpha
        Pass
        {
            Name "MSAA + Polychromatic Alpha"

            Cull Off    ZWrite Off
            Blend Off   // Manual blending
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma multi_compile_fragment NO_WATER SUPPORT_WATER SUPPORT_WATER_CAUSTICS SUPPORT_WATER_CAUSTICS_SHADOW
                #pragma vertex Vert
                #pragma fragment FragMSAAPolychromatic
            ENDHLSL
        }

        // Fog debugging passes
        Pass
        {
            Name "DebugNoMSAA"

            Cull Off    ZWrite Off
            Blend One Zero
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "DebugMSAA"

            Cull Off    ZWrite Off
            Blend One Zero
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragMSAA
            ENDHLSL
        }
        // No debug passes for pbr fog since it is currently disabled
    }
}
