Shader "Hidden/HDRP/Picking"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitProperties.hlsl"

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

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma editor_sync_compilation

            #pragma vertex Vert
            #pragma fragment Frag

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENEPICKINGPASS

            // ==================================================================================================================

            #ifdef DOTS_INSTANCING_ON

            // ========================================================================================================================
            // DOTS PICKING
            // ========================================================================================================================

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct PickingAttributesMesh
            {
                float3 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PickingMeshToDS
            {
                float3 positionRWS : INTERNALTESSPOS;
                float3 normalWS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PickingMeshToPS
            {
                float4 positionCS : SV_Position;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #ifdef TESSELLATION_ON
            #define PickingVertexOutput PickingMeshToDS
            #else
            #define PickingVertexOutput PickingMeshToPS
            #endif

            float4x4 _DOTSPickingViewMatrix;
            float4x4 _DOTSPickingProjMatrix;
            float4 _DOTSPickingCameraWorldPos;

            #undef unity_ObjectToWorld
            float4x4 LoadObjectToWorldMatrixDOTSPicking()
            {
                float4x4 objectToWorld = LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME(float3x4, unity_ObjectToWorld));
            #if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
                objectToWorld._m03_m13_m23 -= _DOTSPickingCameraWorldPos.xyz;
            #endif
                return objectToWorld;
            }

            #undef unity_WorldToObject
            float4x4 LoadWorldToObjectMatrixDOTSPicking()
            {
                float4x4 worldToObject = LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME(float3x4, unity_WorldToObject));

            #if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
                // To handle camera relative rendering we need to apply translation before converting to object space
                float4x4 translationMatrix =
                {
                    { 1.0, 0.0, 0.0, _DOTSPickingCameraWorldPos.x },
                    { 0.0, 1.0, 0.0, _DOTSPickingCameraWorldPos.y },
                    { 0.0, 0.0, 1.0, _DOTSPickingCameraWorldPos.z },
                    { 0.0, 0.0, 0.0, 1.0 }
                };
                return mul(worldToObject, translationMatrix);
            #else
                return worldToObject;
            #endif
            }

            PickingVertexOutput Vert(PickingAttributesMesh input)
            {
                PickingVertexOutput output;
                ZERO_INITIALIZE(PickingVertexOutput, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4x4 objectToWorld = LoadObjectToWorldMatrixDOTSPicking();
                float4 positionWS = mul(objectToWorld, float4(input.positionOS, 1.0));

            #ifdef TESSELLATION_ON
                float4x4 worldToObject = LoadWorldToObjectMatrixDOTSPicking();
                // Normal need to be multiply by inverse transpose
                float3 normalWS = SafeNormalize(mul(input.normalOS, (float3x3)worldToObject));

                output.positionRWS = positionWS;
                output.normalWS = normalWS;
            #else
                output.positionCS = mul(_DOTSPickingProjMatrix, mul(_DOTSPickingViewMatrix, positionWS));
            #endif

                return output;
            }

            #ifdef TESSELLATION_ON

            // AMD recommand this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
            #if defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL)
            #define MAX_TESSELLATION_FACTORS 15.0
            #else
            #define MAX_TESSELLATION_FACTORS 64.0
            #endif

            struct PickingTessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            PickingTessellationFactors HullConstant(InputPatch<PickingMeshToDS, 3> input)
            {
                float3 p0 = input[0].positionRWS;
                float3 p1 = input[1].positionRWS;
                float3 p2 = input[2].positionRWS;

                float3 n0 = input[0].normalWS;
                float3 n1 = input[1].normalWS;
                float3 n2 = input[2].normalWS;

                // ref: http://reedbeta.com/blog/tess-quick-ref/
                // x - 1->2 edge
                // y - 2->0 edge
                // z - 0->1 edge
                // w - inside tessellation factor
                float4 tf = GetTessellationFactors(p0, p1, p2, n0, n1, n2);
                PickingTessellationFactors output;
                output.edge[0] = min(tf.x, MAX_TESSELLATION_FACTORS);
                output.edge[1] = min(tf.y, MAX_TESSELLATION_FACTORS);
                output.edge[2] = min(tf.z, MAX_TESSELLATION_FACTORS);
                output.inside  = min(tf.w, MAX_TESSELLATION_FACTORS);

                return output;
            }

            [maxtessfactor(MAX_TESSELLATION_FACTORS)]
            [domain("tri")]
            [partitioning("fractional_odd")]
            [outputtopology("triangle_cw")]
            [patchconstantfunc("HullConstant")]
            [outputcontrolpoints(3)]
            PickingMeshToDS Hull(InputPatch<PickingMeshToDS, 3> input, uint id : SV_OutputControlPointID)
            {
                // Pass-through
                return input[id];
            }

            PickingMeshToDS PickingInterpolateWithBaryCoordsMeshToDS(PickingMeshToDS input0, PickingMeshToDS input1, PickingMeshToDS input2, float3 baryCoords)
            {
                PickingMeshToDS output;
                UNITY_TRANSFER_INSTANCE_ID(input0, output);

                output.positionRWS = input0.positionRWS * baryCoords.x +  input1.positionRWS * baryCoords.y +  input2.positionRWS * baryCoords.z;
                output.normalWS = input0.normalWS * baryCoords.x +  input1.normalWS * baryCoords.y +  input2.normalWS * baryCoords.z;

                return output;
            }

            PickingMeshToPS PickingVertMeshTesselation(PickingMeshToDS input)
            {
                PickingMeshToPS output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = mul(_DOTSPickingProjMatrix, mul(_DOTSPickingViewMatrix, float4(input.positionRWS, 1)));

                return output;
            }

            [domain("tri")]
            PickingMeshToPS Domain(PickingTessellationFactors tessFactors, const OutputPatch<PickingMeshToDS, 3> input, float3 baryCoords : SV_DomainLocation)
            {
                PickingMeshToDS input0 = input[0];
                PickingMeshToDS input1 = input[1];
                PickingMeshToDS input2 = input[2];

                PickingMeshToDS interpolated = PickingInterpolateWithBaryCoordsMeshToDS(input0, input1, input2, baryCoords);

                return PickingVertMeshTesselation(interpolated);
            }

            #endif // TESSELLATION_ON

            void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                outColor = _SelectionID;
            }

            #else
            // ========================================================================================================================
            // GAMEOBJECT PICKING
            // ========================================================================================================================

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

            PackedVaryingsType Vert(AttributesMesh inputMesh)
            {
                VaryingsType varyingsType;
                varyingsType.vmesh = VertMesh(inputMesh);

                return PackVaryingsType(varyingsType);
            }

            #ifdef TESSELLATION_ON

            PackedVaryingsToPS VertTesselation(VaryingsToDS input)
            {
                VaryingsToPS output;
                output.vmesh = VertMeshTesselation(input.vmesh);
                return PackVaryingsToPS(output);
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

            #endif // TESSELLATION_ON

            void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
                FragInputs input = UnpackVaryingsToFragInputs(packedInput);

                outColor = _SelectionID;
            }

            #endif

            ENDHLSL
        }
    }
}
