Shader "Hidden/DebugVTBlit"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D_X(_BlitTexture);
        TEXTURE2D_X_MSAA(float4, _BlitTextureMSAA);

        Varyings vert(Attributes input)
        {
            Varyings o;
            o.vertex = GetFullScreenTriangleVertexPosition(input.vertexID);
            o.uv = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
            return o;
        }

        float4 ComputeDebugColor(float4 col)
        {
            float tileX = col.x + fmod(col.y, 8.0f) * 256.0f;
            float tileY = floor(col.y / 8.0f) + fmod(col.z, 64.0f) * 32.0f;
            float level = floor((col.z) / 64.0f) + fmod(col.w, 4.0f) * 4.0f;
            float tex = floor(col.w / 4.0f);

            return float4(tileX, tileY, level, tex);
        }

        float4 frag(Varyings i) : SV_Target
        {
            float4 col = 255.0f * SAMPLE_TEXTURE2D_X(_BlitTexture, s_point_clamp_sampler, i.uv);
            return ComputeDebugColor(col);
        }

        float4 fragMSAA(Varyings i) : SV_Target
        {
            float4 col = 255.0f * LOAD_TEXTURE2D_X_MSAA(_BlitTextureMSAA, uint2(i.uv * _ScreenSize.xy / _RTHandleScale.xy), 0);
            return ComputeDebugColor(col);
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        // No culling or depth
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment fragMSAA
                #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            ENDHLSL
        }
    }
    Fallback Off
}
