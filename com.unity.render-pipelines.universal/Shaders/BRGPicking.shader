Shader "Hidden/Universal Render Pipeline/BRGPicking"
{
    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "ShaderModel"="4.5"}
        LOD 300

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            Cull [_CullMode]

            HLSLPROGRAM

            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            #pragma editor_sync_compilation
            #pragma multi_compile DOTS_INSTANCING_ON

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4x4 unity_BRGPickingViewMatrix;
            float4x4 unity_BRGPickingProjMatrix;
            float4 unity_BRGPickingSelectionID;

            float4 ComputeEntityPickingValue(uint entityID)
            {
                // Add 1 to the ID, so the entity ID 0 gets a value that is not equal
                // to the clear value.
                uint pickingValue = (entityID + 1);// | (1 << 31);
                return PackId32ToRGBA8888(pickingValue);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 positionWS = mul(UNITY_MATRIX_M, input.positionOS);
                float4 positionVS = mul(unity_BRGPickingViewMatrix, positionWS);
                output.positionCS = mul(unity_BRGPickingProjMatrix, positionVS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return ComputeEntityPickingValue(unity_EntityId.x);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
