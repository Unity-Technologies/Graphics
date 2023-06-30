Shader "Hidden/SRP/BlitCubeTextureFace"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        // Cubemap blit.  Takes a face index.
        Pass
        {

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma prefer_hlslcc gles
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            TEXTURECUBE(_InputTex);
            SAMPLER(sampler_InputTex);
            float4 _InputTex_HDR;

            float _FaceIndex;
            float _LoD;

            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 texcoord     : TEXCOORD0;
            };

            static const float3 faceU[6] = { float3(0, 0, -1), float3(0, 0, 1), float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(-1, 0, 0) };
            static const float3 faceV[6] = { float3(0, -1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1), float3(0, -1, 0), float3(0, -1, 0) };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
                uv = uv * 2 - 1;

                int idx = (int)_FaceIndex;
                float3 transformU = faceU[idx];
                float3 transformV = faceV[idx];

                float3 n = cross(transformV, transformU);
                output.texcoord = n + uv.x * transformU + uv.y * transformV;
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURECUBE_LOD(_InputTex, sampler_InputTex, input.texcoord, _LoD);
                color.rgb = DecodeHDREnvironment(color, _InputTex_HDR);
                return color;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
