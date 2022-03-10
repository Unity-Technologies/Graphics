Shader "Hidden/HDRP/BRGPicking"
{
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

            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma editor_sync_compilation
            #pragma multi_compile DOTS_INSTANCING_ON

            #pragma vertex Vert
            #pragma fragment Frag

            #define SCENEPICKINGPASS

            float4x4 unity_BRGPickingViewMatrix;
            float4x4 unity_BRGPickingProjMatrix;
            float4 _SelectionID;
            int unity_SubmeshIndex;

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

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

                float4x4 objectToWorld = LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME(float3x4, unity_ObjectToWorld));

                float4 positionWS = mul(objectToWorld, float4(input.positionOS, 1.0));
                float4 positionVS = mul(unity_BRGPickingViewMatrix, positionWS);
                output.positionCS = mul(unity_BRGPickingProjMatrix, positionVS);

                return output;
            }

            void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                outColor = UNITY_ACCESS_DOTS_INSTANCED_SELECTION_VALUE(unity_EntityId, unity_SubmeshIndex);
            }

            ENDHLSL
        }
    }
}
