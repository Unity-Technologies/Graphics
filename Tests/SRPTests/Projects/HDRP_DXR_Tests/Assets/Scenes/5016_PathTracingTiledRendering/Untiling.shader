Shader "RenderStudio/Untiling"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _TileCount ("TileCount", int) = 0
        _Spread ("Spread", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {

            HLSLPROGRAM

            #pragma only_renderers d3d11 vulkan metal
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            // Unity builtin, size of the given texture
            float4 _MainTex_TexelSize;

            // source texture
            Texture2D _MainTex;
            SamplerState my_point_clamp_sampler;

            float _TileCount;
            float _Spread;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 texcoord = input.texcoord.xy;

                uint tileCount = _TileCount;
                float spread = _Spread;
                int pixelX = texcoord.x * _MainTex_TexelSize.z;
                int pixelY = texcoord.y * _MainTex_TexelSize.w;
                int tileIndexX = tileCount - 1 - (pixelX - (pixelX / tileCount) * tileCount);
                int tileIndexY = tileCount - 1 - (pixelY - (pixelY / tileCount) * tileCount);
                int tileWidth = _MainTex_TexelSize.z / tileCount;
                int tileHeight = _MainTex_TexelSize.w / tileCount;

                float2 origin =
                    float2(tileIndexX * tileWidth + (pixelX + spread) / tileCount,
                           tileIndexY * tileHeight + (pixelY + spread) / tileCount)
                    / float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
                return _MainTex.Sample(my_point_clamp_sampler, origin);
                // return float4(1,1,0,0);
            }

            ENDHLSL
        }
    }
}
