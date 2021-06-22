Shader "Hidden/HDRP/VisibilityBuffer"
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
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma enable_d3d11_debug_symbols

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        struct appdata
        {
            float4 vertex : POSITION;
            uint vertexID : SV_VertexID;
            uint instanceID : SV_InstanceID;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            uint cinstanceID : CUSTOM_INSTANCE_ID;
            float3 posWS : POSITION_WS;
        };


        v2f vert(appdata v)
        {
            v2f o;
            ZERO_INITIALIZE(v2f, o);
#ifdef UNITY_ANY_INSTANCING_ENABLED

            UNITY_SETUP_INSTANCE_ID(v);

            uint instanceID = v.instanceID;
            InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];
            int triangleID = v.vertexID / 3;
            int vertexID = v.vertexID % 3;

            uint indexShift = instanceVData.chunkStartIndex * CLUSTER_SIZE_IN_INDICES;
            uint index = _CompactedIndexBuffer[indexShift + triangleID * 3 + vertexID];

            CompactVertex vertex = _CompactedVertexBuffer[index];

            float4x4 m = ApplyCameraTranslationToMatrix(instanceVData.localToWorld);
            float3 posWS = mul(m, float4(vertex.pos, 1.0));

            if (index != 0xffffffff)
            {
                o.cinstanceID = v.instanceID;
                o.vertex = mul(UNITY_MATRIX_VP, float4(posWS, 1.0));
                o.posWS = posWS;
            }
#endif
            return o;
        }


        void frag(v2f packedInput,
            uint primitiveID : SV_PrimitiveID,
            out uint VBuffer0 : SV_Target0,
            out uint VBuffer1 : SV_Target1,
            out float MaterialDepth : SV_Target2
            )
        {
            UNITY_SETUP_INSTANCE_ID(i);
#ifdef UNITY_ANY_INSTANCING_ENABLED

            // Fetch triangle ID (32 bits)
            uint triangleId = primitiveID;

            // Fetch the Geometry ID (16 bits compressed)
            uint instanceID = packedInput.cinstanceID;

            InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];

            // Fetch the Material ID
            uint materialId = instanceVData.materialIndex;
            // Write the VBuffer
            VBuffer0 = triangleId;
            VBuffer1 = instanceID;
            MaterialDepth = materialId;
#else
            VBuffer0 = 0;
            VBuffer1 = 0;
            MaterialDepth = 0;
#endif
        }
        ENDHLSL

        Pass
        {
            Name "VisibilityBuffer"
            Tags{ "LightMode" = "VisibilityBuffer" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
