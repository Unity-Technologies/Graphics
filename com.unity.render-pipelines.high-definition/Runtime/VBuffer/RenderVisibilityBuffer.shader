Shader "Hidden/HDRP/VisibilityBuffer"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }
        LOD 100

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        struct appdata
        {
            float4 vertex : POSITION;
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        v2f vert(appdata v)
        {
            v2f o;
            ZERO_INITIALIZE(v2f, o);
#if UNITY_ANY_INSTANCING_ENABLED

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);

            uint instanceID = UNITY_GET_INSTANCE_ID(v);
            InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];
            int triangleID = v.vertexID / 3;
            int vertexID = v.vertexID % 3;

            uint index = _CompactedIndexBuffer[instanceVData.startIndex + triangleID * 3 + v.vertexID];

            CompactVertex vertex = _CompactedVertexBuffer[index];

            float4x4 m = ApplyCameraTranslationToMatrix(instanceVData.localToWorld);
            float3 posWS = mul(m, float4(vertex.pos, 1.0));
            o.vertex = mul(UNITY_MATRIX_VP, float4(posWS, 1.0));
#else
            return o;
#endif
        }


        void frag(v2f packedInput,
            uint primitiveID : SV_PrimitiveID,
            out uint VBuffer0 : SV_Target0,
            out uint VBuffer1 : SV_Target1,
            out float MaterialDepth : SV_Target2
            )
        {
            UNITY_SETUP_INSTANCE_ID(i);
#if UNITY_ANY_INSTANCING_ENABLED

            // Fetch triangle ID (32 bits)
            uint triangleId = primitiveID;

            // Fetch the Geometry ID (16 bits compressed)
            uint instanceID = UNITY_GET_INSTANCE_ID(packedInput);

            InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];

            // Fetch the Material ID
            uint materialId = instanceVData.materialIndex;
            // Write the VBuffer
            VBuffer0 = triangleId;
            VBuffer1 = instanceID & 0xffff;
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
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
