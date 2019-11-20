Shader "Hidden/HDRP/OpaqueAtmosphericScattering"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #pragma multi_compile _ DEBUG_DISPLAY

        // #pragma enable_d3d11_debug_symbols

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

        TEXTURE2D_X_MSAA(float4, _ColorTextureMS);
        TEXTURE2D_X_MSAA(float,  _DepthTextureMS);
        TEXTURE2D_X(_ColorTexture);

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

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        void AtmosphericScatteringCompute(Varyings input, float3 V, float depth, out float3 color, out float3 opacity)
        {
            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

            if (depth == UNITY_RAW_FAR_CLIP_VALUE)
            {
                // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
                // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
                // And recompute the position on the sphere with the current camera direction.
                posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;

                // Warning: we do not modify depth values. Use them with care!
            }

            EvaluateAtmosphericScattering(posInput, V, color, opacity); // Premultiplied alpha
        }

        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS  = input.positionCS.xy;
            float3 V           = GetSkyViewDirWS(positionSS);
            float  depth       = LoadCameraDepth(positionSS);
            float3 surfColor   = LOAD_TEXTURE2D_X(_ColorTexture, (int2)positionSS).rgb;

            float3 volColor, volOpacity;
            AtmosphericScatteringCompute(input, V, depth, volColor, volOpacity);

            return float4(volColor, volOpacity.x);
        }

        float4 FragMSAA(Varyings input, uint sampleIndex: SV_SampleIndex) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS  = input.positionCS.xy;
            float3 V           = GetSkyViewDirWS(positionSS);
            float  depth       = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, (int2)positionSS, sampleIndex).x;
            float3 surfColor   = LOAD_TEXTURE2D_X_MSAA(_ColorTextureMS, (int2)positionSS, sampleIndex).rgb;

            float3 volColor, volOpacity;
            AtmosphericScatteringCompute(input, V, depth, volColor, volOpacity);

            return float4(volColor, volOpacity.x);
        }

        float4 FragPBRFog(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS = input.positionCS.xy;
            float3 V = GetSkyViewDirWS(positionSS);
            float  depth = LoadCameraDepth(positionSS);
            float3 surfColor = LOAD_TEXTURE2D_X(_ColorTexture, (int2)positionSS).rgb;

            float3 volColor, volOpacity;
            AtmosphericScatteringCompute(input, V, depth, volColor, volOpacity);

            return float4(volColor + (1 - volOpacity) * surfColor, 1); // Premultiplied alpha (over operator)
        }

            float4 FragMSAAPBRFog(Varyings input, uint sampleIndex: SV_SampleIndex) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 positionSS = input.positionCS.xy;
            float3 V = GetSkyViewDirWS(positionSS);
            float  depth = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, (int2)positionSS, sampleIndex).x;
            float3 surfColor = LOAD_TEXTURE2D_X_MSAA(_ColorTextureMS, (int2)positionSS, sampleIndex).rgb;

            float3 volColor, volOpacity;
            AtmosphericScatteringCompute(input, V, depth, volColor, volOpacity);

            return float4(volColor + (1 - volOpacity) * surfColor, 1); // Premultiplied alpha (over operator)
        }
    ENDHLSL

    SubShader
    {
        // 0: NOMSAA
        Pass
        {
            Cull Off    ZWrite Off
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }

        // 1: MSAA
        Pass
        {
            Cull Off    ZWrite Off
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragMSAA
            ENDHLSL
        }

            // 2: NOMSAA PBR FOG
            Pass
        {
            Cull Off    ZWrite Off
            Blend Off   // Manual blending
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPBRFog
            ENDHLSL
        }

            // 3: MSAA PBR FOG
            Pass
        {
            Cull Off    ZWrite Off
            Blend Off   // Manual blending
            ZTest Less  // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragMSAAPBRFog
            ENDHLSL
        }
    }
}
