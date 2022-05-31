Shader "Hidden/HDRP/ScreenSpaceShadows"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
        #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

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
            output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float depth = LoadCameraDepth(input.positionCS.xy);

            if (depth == UNITY_RAW_FAR_CLIP_VALUE)
                return 1.0f;

            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

            // Adjust world-space position for XR single-pass and camera relative
            ApplyCameraRelativeXR(posInput.positionWS);

            // Init shadow context
            LightLoopContext context;
            context.shadowContext = InitShadowContext();

            // Get directional light data. By definition we only have one directional light casting shadow
            DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];
            float3 L = -light.forward;

            // We also need the normal
            NormalData normalData;
            DecodeFromNormalBuffer(posInput.positionSS.xy, normalData);
            float3 normalWS = normalData.normalWS;

            // Note: we use shading normal here and not GetNormalForShadowBias() as it is not available
            return GetDirectionalShadowAttenuation(context.shadowContext, posInput.positionSS.xy, posInput.positionWS, normalWS, light.shadowIndex, L);
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
