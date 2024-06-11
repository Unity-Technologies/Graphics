Shader "Hidden/HDRP/OpaqueAtmosphericScattering"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        //#pragma enable_d3d11_debug_symbols

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
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

        #ifdef DEBUG_DISPLAY
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #endif

        TEXTURE2D_X_MSAA(float4, _ColorTextureMS);
        TEXTURE2D_X_MSAA(float,  _DepthTextureMS);
        TEXTURE2D_X(_ColorTexture);
        float _MultipleScatteringIntensity;

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
            float2 fogTransmittance : SV_Target1;
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

        PositionInputs GetPositionInput(Varyings input, float depth)
        {
            return GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        }

        FragOutput ComputeFragmentOutput(float4 color, float3 fogOpacity, float3 debugColor)
        {
            FragOutput output;

            output.color = color;

            #if defined(OUTPUT_TRANSMITTANCE_BUFFER)
            float finalOpacity = (fogOpacity.x + fogOpacity.y + fogOpacity.z) / 3.0f;
            output.fogTransmittance = 1 - finalOpacity;
            #endif

            #ifdef DEBUG_DISPLAY
            if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VOLUMETRIC_FOG)
                output.color = float4(debugColor, 0.0f);
            #endif

            return output;
        }

        // Helpers to reduce duplication
        FragOutput OutputFog(float3 volColor, float3 volOpacity)
        {
            return ComputeFragmentOutput(float4(volColor, 1.0 - volOpacity.x), volOpacity, volColor);
        }

        FragOutput OutputFog(float4 surfColor, float3 volColor, float3 volOpacity, float3 fogOpacity)
        {
            // Premultiplied alpha (over operator), preserve alpha for the alpha channel for compositing
            return ComputeFragmentOutput(float4(volColor + (1 - volOpacity) * surfColor.rgb, surfColor.a), fogOpacity, volColor);
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

                #define ATMOSPHERE_NO_AERIAL_PERSPECTIVE
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

                #pragma vertex Vert
                #pragma fragment Frag

                FragOutput Frag(Varyings input)
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    float2 positionSS = input.positionCS.xy;
                    float3 V          = GetSkyViewDirWS(positionSS);
                    float  depth      = LoadCameraDepth(positionSS);

                    PositionInputs posInput = GetPositionInput(input, depth);

                    float3 volColor, volOpacity;
                    EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);

                    return OutputFog(volColor, volOpacity);
                }
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

                #define ATMOSPHERE_NO_AERIAL_PERSPECTIVE
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

                #pragma vertex Vert
                #pragma fragment FragMSAA

                FragOutput FragMSAA(Varyings input, uint sampleIndex: SV_SampleIndex)
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    float2 positionSS = input.positionCS.xy;
                    float3 V          = GetSkyViewDirWS(positionSS);
                    float  depth      = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, (int2)positionSS, sampleIndex).x;

                    PositionInputs posInput = GetPositionInput(input, depth);

                    float3 volColor, volOpacity;
                    EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);

                    return OutputFog(volColor, volOpacity);
                }
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

                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

                #pragma vertex Vert
                #pragma fragment FragPolychromatic

                FragOutput FragPolychromatic(Varyings input)
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    float2 positionSS = input.positionCS.xy;
                    float3 V          = GetSkyViewDirWS(positionSS);
                    float  depth      = LoadCameraDepth(positionSS);

                    PositionInputs posInput = GetPositionInput(input, depth);

                    float3 volColor, volOpacity, fogOpacity = 0.0f;
                    if (EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity))
                        fogOpacity = volOpacity;

                    float4 surfColor = LOAD_TEXTURE2D_X(_ColorTexture, (int2)positionSS);
                    return OutputFog(surfColor, volColor, volOpacity, fogOpacity);
                }
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

                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

                #pragma vertex Vert
                #pragma fragment FragMSAAPolychromatic

                FragOutput FragMSAAPolychromatic(Varyings input, uint sampleIndex: SV_SampleIndex)
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    float2 positionSS = input.positionCS.xy;
                    float3 V          = GetSkyViewDirWS(positionSS);
                    float  depth      = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, (int2)positionSS, sampleIndex).x;

                    PositionInputs posInput = GetPositionInput(input, depth);

                    float3 volColor, volOpacity, fogOpacity = 0.0f;
                    if (EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity))
                        fogOpacity = volOpacity;

                    float4 surfColor = LOAD_TEXTURE2D_X_MSAA(_ColorTextureMS, (int2)positionSS, sampleIndex);
                    return OutputFog(surfColor, volColor, volOpacity, fogOpacity);
                }
            ENDHLSL
        }
    }
}
