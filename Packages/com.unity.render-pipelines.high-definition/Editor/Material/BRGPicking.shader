
// Use this shader as a fallback when trying to render using a BatchRendererGroup with a shader that doesn't define a ScenePickingPass or SceneSelectionPass.
Shader "Hidden/HDRP/BRGPicking"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma editor_sync_compilation
    #pragma multi_compile DOTS_INSTANCING_ON
    //#pragma enable_d3d11_debug_symbols

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline"="HDRenderPipeline" "RenderType" = "HDLitShader" }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            Cull [_CullMode]

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #define SCENEPICKINGPASS

            // ScenePickingPass uses the old built-in matrix property
            float4x4 unity_MatrixVP;
            float4 _SelectionID;

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            struct PickingAttributesMesh
            {
                float3 positionOS   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PickingMeshToPS
            {
                float4 positionCS : SV_Position;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #undef unity_ObjectToWorld

            PickingMeshToPS Vert(PickingAttributesMesh input)
            {
                PickingMeshToPS output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4x4 objectToWorld = UNITY_DOTS_MATRIX_M;

                float4 positionWS = mul(objectToWorld, float4(input.positionOS, 1.0));
                output.positionCS = mul(unity_MatrixVP, positionWS);

                return output;
            }

            void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                outColor = unity_SelectionID;
            }

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

            // enable dithering LOD crossfade
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            int _ObjectId;
            int _PassValue;

            struct SurfaceData
            {
            };

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            //#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

            void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                builtinData = (BuiltinData)0;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}
