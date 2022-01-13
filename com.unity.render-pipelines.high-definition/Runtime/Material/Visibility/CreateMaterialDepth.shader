Shader "Hidden/HDRP/CreateMaterialDepth"
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
        //#pragma enable_d3d11_debug_symbols

        #define DOTS_INSTANCING_ON 1

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/GeometryPool/Resources/GeometryPool.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityCommon.hlsl"

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        Varyings Vert(Attributes inputMesh)
        {
            Varyings output;
            ZERO_INITIALIZE(Varyings, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(inputMesh.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(inputMesh.vertexID);
            return output;
        }

        void Frag(Varyings input, out float outDepth : SV_Depth)
        {
            Visibility::VisibilityData visData = Visibility::LoadVisibilityData(input.positionCS.xy);
            outDepth = 0.0f;
            if (!visData.valid)
            {
                clip(-1);
                return;
            }

            uint materialKey = Visibility::GetMaterialKey(visData);
            uint materialBatchKey = (materialKey << 8) | (visData.batchID & 0xff);
            // We assume a maximum of 65536 materials in scene.
            outDepth = Visibility::PackDepthMaterialKey(materialBatchKey);
        }

        ENDHLSL

        Pass
        {
            Name "CreateMaterialDepth"
            Tags{ "LightMode" = "CreateMaterialDepth" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
