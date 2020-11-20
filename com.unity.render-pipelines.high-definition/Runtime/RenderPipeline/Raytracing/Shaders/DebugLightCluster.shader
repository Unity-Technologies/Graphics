Shader "Hidden/HDRP/DebugLightCluster"
{
    SubShader
    {
        Tags { "Queue"="Transparent+0" "IgnoreProjector"="True" "RenderType"="Transparent" }

        HLSLINCLUDE
        #pragma only_renderers d3d11

        static const float3 cubeVertices[24] =
        {
            // Bottom Face
            float3(0.0, 0.0, 0.0),
            float3(0.0, 1.0, 0.0),
            float3(1.0, 1.0, 0.0),
            float3(1.0, 0.0, 0.0),

            // Left Face
            float3(0.0, 0.0, 0.0),
            float3(1.0, 0.0, 0.0),
            float3(1.0, 0.0, 1.0),
            float3(0.0, 0.0, 1.0),

            // Front Face
            float3(1.0, 0.0, 0.0),
            float3(1.0, 1.0, 0.0),
            float3(1.0, 1.0, 1.0),
            float3(1.0, 0.0, 1.0),

            // Right Face
            float3(0.0, 1.0, 0.0),
            float3(0.0, 1.0, 1.0),
            float3(1.0, 1.0, 1.0),
            float3(1.0, 1.0, 0.0),

            // Back Face
            float3(0.0, 0.0, 0.0),
            float3(0.0, 0.0, 1.0),
            float3(0.0, 1.0, 1.0),
            float3(0.0, 1.0, 0.0),

            // Top Face
            float3(0.0, 0.0, 1.0),
            float3(1.0, 0.0, 1.0),
            float3(1.0, 1.0, 1.0),
            float3(0.0, 1.0, 1.0)
        };


        static const int cubeLines[48] =
        {
            // Bottom Face
            0, 1, 1, 2, 2, 3, 3, 0,

            // Left Face
            4, 5, 5, 6, 6, 7, 7, 4,

            // Front Face
            8, 9, 9, 10, 10, 11, 11, 8,

            // Right Face
            12, 13, 13, 14, 14, 15, 15, 12,

            // Back Face
            16, 17, 17, 18, 18, 19, 19, 16,

            // Top Face
            20, 21, 21, 22, 22, 23, 23, 20
        };


        static const int cubeTriangles[36] =
        {
            // Bottom Face
            0, 1, 2, 2, 3, 0,

            // Left Face
            4, 5, 6, 4, 6, 7,

            // Front Face
            8, 9, 10, 8, 10, 11,

            // Right Face
            12, 13, 14, 12, 14, 15,

            // Back Face
            16, 17, 18, 16, 18, 19,

            // Top Face
            20, 21, 22, 20, 22, 23
        };

        ENDHLSL

        Pass
        {
            Cull Back
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingLightCluster.hlsl"

            struct AttributesDefault
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDefault
            {
                float4 positionCS : SV_POSITION;
                float4 cellColor : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsDefault vert(AttributesDefault att, uint vertID : SV_VertexID, uint instanceID: SV_InstanceID)
            {
                VaryingsDefault output;
                UNITY_SETUP_INSTANCE_ID(att);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Compute the csize of each cell
                float3 clusterCellSize = (_MaxClusterPos - _MinClusterPos) * float3(1.0 / 64.0, 1.0 / 64.0, 1.0 / 32.0);

                // Compute the camera relative position
                float3 positionOS = cubeVertices[cubeTriangles[vertID % 48]];
                positionOS *= clusterCellSize;
                float3 positionRWS = TransformObjectToWorld(positionOS);

                // Compute the grid coordinates of this cell
                int width = instanceID % 64;
                int height = (instanceID / 64) % 64;
                int depth = instanceID / 4096;

                // Compute the world space coordinate of this cell
                positionRWS = _MinClusterPos + float3( clusterCellSize.x * width, clusterCellSize.y * height, clusterCellSize.z * depth) + GetAbsolutePositionWS(positionRWS);

                // Given that we have the cell index, get the number of lights
                uint numLights = GetTotalLightClusterCellCount(depth + height * 32 + width * 2048);
                output.cellColor.xyz = lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 0.0), clamp((float) numLights / _LightPerCellCount, 0.0, 1.0));
                output.cellColor.w = numLights == 0 ? 0.0 : 1.0;
                output.cellColor.xyz = numLights >= _LightPerCellCount ?  float3(5.0, 0.0, 0.0) : output.cellColor.xyz;

                // Compute the clip space position
                output.positionCS = TransformWorldToHClip(positionRWS);
                return output;
            }

            void frag(VaryingsDefault varying, out float4 outCellColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varying);
                outCellColor = float4(varying.cellColor.xyz * varying.cellColor.w / 50, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Cull Back
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingLightCluster.hlsl"


            struct AttributesDefault
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDefault
            {
                float4 positionCS : SV_POSITION;
                float4 cellColor : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsDefault vert(AttributesDefault att, uint vertID : SV_VertexID, uint instanceID: SV_InstanceID)
            {
                VaryingsDefault output;
                UNITY_SETUP_INSTANCE_ID(att);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Compute the csize of each cell
                float3 clusterCellSize = (_MaxClusterPos - _MinClusterPos) * float3(1.0 / 64.0, 1.0 / 64.0, 1.0 / 32.0);

                // Compute the camera relative position
                float3 positionOS = cubeVertices[cubeLines[vertID % 48]];
                positionOS *= clusterCellSize;
                float3 positionRWS = TransformObjectToWorld(positionOS);

                // Compute the grid coordinates of this cell
                int width = instanceID % 64;
                int height = (instanceID / 64) % 64;
                int depth = instanceID / 4096;

                // Compute the world space coordinate of this cell
                positionRWS = _MinClusterPos + float3( clusterCellSize.x * width, clusterCellSize.y * height, clusterCellSize.z * depth) + GetAbsolutePositionWS(positionRWS);

                // Given that we have the cell index, get the number of lights
                uint numLights = GetTotalLightClusterCellCount(depth + height * 32 + width * 2048);
                output.cellColor.xyz = lerp(float3(0.0, 1.0, 0.0), float3(1.0, 1.0, 0.0), clamp((float) numLights / _LightPerCellCount, 0.0, 1.0));
                output.cellColor.w = numLights == 0 ? 0.0 : 1.0;
                output.cellColor.xyz = numLights >= _LightPerCellCount ?  float3(1.0, 0.0, 0.0) : output.cellColor.xyz;

                // Compute the clip space position
                output.positionCS = TransformWorldToHClip(positionRWS);
                return output;
            }

            void frag(VaryingsDefault varying, out float4 outCellColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varying);
                outCellColor = float4(varying.cellColor.xyz  * varying.cellColor.w / 50 , 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
