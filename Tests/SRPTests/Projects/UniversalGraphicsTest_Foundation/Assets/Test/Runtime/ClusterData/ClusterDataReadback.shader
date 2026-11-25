Shader "Hidden/Test/ClusterDataReadback"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #define _CLUSTER_LIGHT_LOOP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            int _ZBinWordCount;
            int _TileWordCount;
            int _ReadbackWidth;

            float4 Vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition(vertexID);
            }

            float4 EncodeBytes(uint value)
            {
                return float4(value & 0xFF, (value >> 8) & 0xFF, (value >> 16) & 0xFF, value >> 24) / 255.0;
            }

            static const uint k_ConstantCount = 11;
            static const uint k_ConstantFlag = 0x80000000u;

            // The cluster constants as the cluster light loop reads them, floats as raw bits.
            uint LoadConstant(uint id)
            {
                switch (id)
                {
                    case 0: return asuint(URP_FP_ZBIN_SCALE);
                    case 1: return asuint(URP_FP_ZBIN_OFFSET);
                    case 2: return URP_FP_PROBES_BEGIN;
                    case 3: return URP_FP_DIRECTIONAL_LIGHTS_COUNT;
                    case 4: return asuint(URP_FP_TILE_SCALE.x);
                    case 5: return asuint(URP_FP_TILE_SCALE.y);
                    case 6: return URP_FP_TILE_COUNT_X;
                    case 7: return URP_FP_TILE_COUNT_Y;
                    case 8: return URP_FP_WORDS_PER_TILE;
                    case 9: return URP_FP_ZBIN_COUNT;
                    default: return URP_FP_TILE_COUNT;
                }
            }

            // Writes the bound z-bins, then the tile masks, then the layout constants, read through the lighting's accessors,
            // as texel pairs: an index (constants are flagged), then the value, each as the four bytes of an RGBA8 texel.
            float4 Frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                uint2 texel = uint2(positionCS.xy);
                uint texelIndex = texel.y * (uint)_ReadbackWidth + texel.x;
                uint index = texelIndex >> 1;
                uint zBinWordCount = (uint)_ZBinWordCount;
                uint wordCount = zBinWordCount + (uint)_TileWordCount;

                uint value = 0;
                if (index < zBinWordCount)
                {
                    value = LoadZBin(index);
                }
                else if (index < wordCount)
                {
                    value = LoadTileWord(index - zBinWordCount);
                }
                else if (index < wordCount + k_ConstantCount)
                {
                    value = LoadConstant(index - wordCount);
                    index = k_ConstantFlag | (index - wordCount);
                }
                else
                {
                    index = 0xFFFFFFFFu;
                }

                return EncodeBytes((texelIndex & 1) == 0 ? index : value);
            }
            ENDHLSL
        }
    }
}
