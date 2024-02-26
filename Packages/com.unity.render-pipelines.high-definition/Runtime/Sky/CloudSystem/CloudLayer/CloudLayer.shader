Shader "Hidden/HDRP/Sky/CloudLayer"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    #pragma multi_compile_local LAYER1_STATIC LAYER1_PROCEDURAL LAYER1_FLOWMAP
    #pragma multi_compile_local LAYER2_OFF LAYER2_STATIC LAYER2_PROCEDURAL LAYER2_FLOWMAP
    #pragma multi_compile_local _ PHYSICALLY_BASED_SUN

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/CloudSystem/CloudLayer/CloudLayerCommon.hlsl"

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
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        float4 result = RenderClouds(input.positionCS.xy);
        result.rgb = ClampToFloat16Max(result.rgb / result.a) * result.a;
        return result;
    }

    #ifdef CLOUD_RENDER_OPACITY_MRT
    struct RenderOutput
    {
        float4 colorBuffer : SV_Target0;
        float4 transmittanceBuffer : SV_Target1;
    };
    #else
    struct RenderOutput
    {
        float4 colorBuffer : SV_Target;
    };
    #endif

    RenderOutput FragRender(Varyings input)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float3 V = GetSkyViewDirWS(input.positionCS.xy);
        float4 color = RenderClouds(-V);
        color.rgb *= GetCurrentExposureMultiplier();
        RenderOutput output;

        if (color.a != 0.0f)
        {
            float linearDepth = IntersectSphere(_LowestAltitude(0), -V.y, _PlanetaryRadius).y;
            float3 positionWS = -V * linearDepth;

            // Compute pos inputs
            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, positionWS);
            posInput.linearDepth = linearDepth * dot(-V, GetViewForwardDir());
            posInput.deviceDepth = UNITY_NEAR_CLIP_VALUE; // unused, just to avoid culling

            // Apply atmospheric fog
            float3 volColor, volOpacity;
            EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);
            color.xyz = color.xyz * (1 - volOpacity) + volColor * color.a;
        }

        output.colorBuffer = color;

        #ifdef CLOUD_RENDER_OPACITY_MRT
        // We always store the total transmittance in the first channel as we don't want to accumulate cloud layers
        // for the opacity used in the fog multiple scattering.
        output.transmittanceBuffer = float4(1 - color.a, 1, 1, 1);
        #endif

        return output;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend 0 One OneMinusSrcAlpha // Premultiplied alpha
            Blend 1 DstColor Zero
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend 0 One OneMinusSrcAlpha // Premultiplied alpha
            Blend 1 DstColor Zero
            Cull Off

            HLSLPROGRAM
                #pragma multi_compile _ CLOUD_RENDER_OPACITY_MRT
                #pragma fragment FragRender
            ENDHLSL
        }

    }
    Fallback Off
}
