Shader "Hidden/HDRP/TemporalAntialiasing"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile_local _ ORTHOGRAPHIC
        #pragma multi_compile_local _ REDUCED_HISTORY_CONTRIB
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/TemporalAntialiasing.hlsl"

        TEXTURE2D_X(_InputTexture);
        TEXTURE2D_X(_InputHistoryTexture);
        RW_TEXTURE2D_X(float3, _OutputHistoryTexture);

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        void FragTAA(Varyings input, out float3 outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float sharpenStrength = _TaaFrameInfo.x;
            float2 jitter = _TaaJitterStrength.zw;

    #if defined(ORTHOGRAPHIC)
            // Don't dilate in ortho
            float2 closest = input.positionCS.xy;
    #else
            float2 closest = GetClosestFragment(input.positionCS.xy);
    #endif

            float2 motionVector;
            DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, closest), motionVector);
            float motionVecLength = length(motionVector);

            float2 uv = input.texcoord - jitter;

            float3 color = Fetch(_InputTexture, uv, 0.0, _RTHandleScale.xy);
            float3 history = Fetch(_InputHistoryTexture, input.texcoord - motionVector, 0.0, _RTHandleScaleHistory.zw);

            float3 topLeft = Fetch(_InputTexture, uv, -RADIUS, _RTHandleScale.xy);
            float3 bottomRight = Fetch(_InputTexture, uv, RADIUS, _RTHandleScale.xy);

            float3 corners = 4.0 * (topLeft + bottomRight) - 2.0 * color;

            // Sharpen output
    #if SHARPEN
            float3 topRight = Fetch(_InputTexture, uv, float2(RADIUS, -RADIUS), _RTHandleScale.xy);
            float3 bottomLeft = Fetch(_InputTexture, uv, float2(-RADIUS, RADIUS), _RTHandleScale.xy);
            float3 blur = (topLeft + topRight + bottomLeft + bottomRight) * 0.25;
            color += (color - blur) * sharpenStrength;
    #endif

            color = clamp(color, 0.0, CLAMP_MAX);

            float3 average = Map((corners + color) / 7.0);

            topLeft = Map(topLeft);
            bottomRight = Map(bottomRight);
            color = Map(color);

            float colorLuma = Luminance(color);
            float averageLuma = Luminance(average);
            float nudge = lerp(4.0, 0.25, saturate(motionVecLength * 100.0)) * abs(averageLuma - colorLuma);

            float3 minimum = min(bottomRight, topLeft) - nudge;
            float3 maximum = max(topLeft, bottomRight) + nudge;

            history = Map(history);

            // Clip history samples
    #if CLIP_AABB
            history = ClipToAABB(history, minimum, maximum);
    #else
            history = clamp(history, minimum, maximum);
    #endif

            // Blend color & history
            // Feedback weight from unbiased luminance diff (Timothy Lottes)
            float historyLuma = Luminance(history);
            float diff = abs(colorLuma - historyLuma) / Max3(colorLuma, historyLuma, 0.2);
            float weight = 1.0 - diff;
            float feedback = lerp(FEEDBACK_MIN, FEEDBACK_MAX, weight * weight);

            color = Unmap(lerp(color, history, feedback));
            color = clamp(color, 0.0, CLAMP_MAX);

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color;
            outColor = color; 
        }

        void FragExcludedTAA(Varyings input, out float3 outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 jitter = _TaaJitterStrength.zw;
            float2 uv = input.texcoord - jitter;

            float3 color = Fetch(_InputTexture, uv, 0.0, _RTHandleScale.xy);

            outColor = color;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // TAA
        Pass
        {
            Stencil
            {
                ReadMask 16     // ExcludeFromTAA
                Ref 16          // ExcludeFromTAA
                Comp NotEqual
                Pass Keep
            }

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragTAA
            ENDHLSL
        }

        // Excluded from TAA
        // Note: This is a straightup passthrough now, but it would be interesting instead to try to reduce history influence instead.
        Pass
        {
            Stencil
            {
                ReadMask 16     // ExcludeFromTAA
                Ref 16          // ExcludeFromTAA
                Comp Equal
                Pass Keep
            }

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragExcludedTAA
            ENDHLSL
        }
    }
    Fallback Off
}
