Shader "Hidden/HDRP/RenderVisibilityBuffer"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        // GPU Instancing
        //#pragma multi_compile_instancing
        //#pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma enable_d3d11_debug_symbols

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/VisibilityBufferCommon.hlsl"

        struct appdata
        {
            float4 vertex : POSITION;
            uint vertexID : SV_VertexID;
            uint instanceID : SV_InstanceID;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            uint globalInstanceID : CUSTOM_INSTANCE_ID;
            float3 posWS : POSITION_WS;
            uint primitiveID : BLENDINDICES0;
        };

        int _InstanceVDataShift;

        v2f vert(appdata v)
        {
            v2f o;
            ZERO_INITIALIZE(v2f, o);
//#ifdef UNITY_ANY_INSTANCING_ENABLED

            //UNITY_SETUP_INSTANCE_ID(v);

            uint globalInstanceID = v.instanceID + _InstanceVDataShift;
            InstanceVData instanceVData = _InstanceVDataBuffer[globalInstanceID];
            int triangleID = v.vertexID / 3;
            int vertexID = v.vertexID % 3;

            uint indexShift = instanceVData.chunkStartIndex * CLUSTER_SIZE_IN_INDICES;
            uint index = _CompactedIndexBuffer[indexShift + triangleID * 3 + vertexID];

            CompactVertex vertex = _CompactedVertexBuffer[index];

            float4x4 m = ApplyCameraTranslationToMatrix(instanceVData.localToWorld);
            float3 posWS = mul(m, float4(vertex.pos, 1.0));

            if (index != 0xffffffff)
            {
                o.globalInstanceID = globalInstanceID;
                o.vertex = mul(UNITY_MATRIX_VP, float4(posWS, 1.0));
                o.posWS = posWS;
                o.primitiveID = triangleID;
            }
//#endif
            return o;
        }

        uint PackVisBuffer(uint clusterID, uint triangleID)
        {
            uint output = 0;
            // Cluster size is 128, hence we need 7 bits at most for triangle ID.
            output = triangleID & 127;
            // All the remaining 25 bits can be used for cluster (for a max of 33554431 (2^25 - 1) clusters)
            output |= (clusterID & 33554431) << 7;
            return output;
        }

        void frag(v2f packedInput,
            out uint VBuffer0 : SV_Target0
            )
        {
//#ifdef UNITY_ANY_INSTANCING_ENABLED
//            UNITY_SETUP_INSTANCE_ID(packedInput);

            // Fetch triangle ID (32 bits)
            uint triangleID = packedInput.primitiveID;

            uint instanceID = packedInput.globalInstanceID;

            InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];

            // Write the VBuffer
            VBuffer0 = PackVisBuffer(instanceID, triangleID);
//#else
//            VBuffer0 = 0;
//#endif
        }
        ENDHLSL

        Pass
        {
            Name "VisibilityBufferB"
            Tags{ "LightMode" = "VisibilityBufferB" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "VisibilityBufferF"
            Tags{ "LightMode" = "VisibilityBufferF" }

            ZWrite On
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "VisibilityBufferN"
            Tags{ "LightMode" = "VisibilityBufferN" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
