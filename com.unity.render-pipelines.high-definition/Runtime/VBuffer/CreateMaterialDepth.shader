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

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/VisibilityBufferCommon.hlsl"

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        Varyings Vert(Attributes inputMesh)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(inputMesh);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(inputMesh.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(inputMesh.vertexID);
            return output;
        }


        void Frag(Varyings input, out float outDepth : SV_Depth)
        {
            // Sample the material buffer and pass it over depth buffer.
            uint materialID = LOAD_TEXTURE2D_X(_MaterialDepth, input.positionCS.xy).x;

            // We assume a maximum of 65536 materials in scene. (R16 UINT is the source).
            outDepth = (float)materialID / 65535;
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
