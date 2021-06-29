Shader "Hidden/HDRP/DebugLocalVolumetricFogAtlas"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma vertex Vert

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE3D(_InputTexture);
        SAMPLER(sampler_InputTexture);
        float _Slice;
        float3 _Offset;
        float3 _TextureSize;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

            return output;
        }

        float3 GetUVs(float2 texcoords)
        {
            return float3(texcoords * float2(1, _TextureSize.x / _TextureSize.y) * _TextureSize.xy, _Slice) + _Offset;
        }

        float4 Color(Varyings input) : SV_Target
        {
            float3 uv = GetUVs(input.texcoord.xy);

            return float4(LOAD_TEXTURE3D_LOD(_InputTexture, uv, 0).rgb, 1);
        }

        float4 Alpha(Varyings input) : SV_Target
        {
            float3 uv = GetUVs(input.texcoord.xy);

            return float4(LOAD_TEXTURE3D_LOD(_InputTexture, uv, 0).aaa, 1);
        }

        ENDHLSL

        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma fragment Color
            ENDHLSL
        }

        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma fragment Alpha
            ENDHLSL
        }
    }
    Fallback Off
}
