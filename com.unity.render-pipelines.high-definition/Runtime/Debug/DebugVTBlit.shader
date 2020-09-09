Shader "Hidden/DebugVTBlit"
{
    SubShader
    {
        // No culling or depth
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            float4 _BlitScaleBias;
            TEXTURE2D_X(_BlitTexture);

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.vertex = GetFullScreenTriangleVertexPosition(input.vertexID);
                o.uv = GetNormalizedFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 col = 255.0f * SAMPLE_TEXTURE2D_X(_BlitTexture, s_point_clamp_sampler, i.uv);

                float tileX = col.x + fmod(col.y, 8.0f) * 256.0f;
                float tileY = floor(col.y / 8.0f) + fmod(col.z, 64.0f) * 32.0f;
                float level = floor((col.z) / 64.0f) + fmod(col.w, 4.0f) * 4.0f;
                float tex = floor(col.w / 4.0f);

                return float4(tileX, tileY, level, tex);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
