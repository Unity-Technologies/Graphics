Shader "Hidden/Core/DebugOcclusionTest"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch webgpu

            #pragma vertex Vert
            #pragma fragment Frag
            //#pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/InstanceOcclusionCuller.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingDebugShaderVariables.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingCommon.cs.hlsl"

            StructuredBuffer<uint> _OcclusionDebugOverlay;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID) * _DepthSizeInOccluderPixels.xy;
                return output;
            }

            // c.f. OverlayHeatMap
            float4 OverlayHeapMapColor(float cost, float costScreenTotal, float opacity)
            {
                // adjust color range. max clamp is to avoid scaling if 12 colors are enough.
                float colorRangeScale = 1.0 / max(1.0, costScreenTotal / (float)DEBUG_COLORS_COUNT);
                int colorIndex = (int)(1.0 + cost * colorRangeScale);

                colorIndex = clamp(colorIndex, 1, DEBUG_COLORS_COUNT-1); // skip colorIndex 0 (black)
                float4 col = kDebugColorGradient[colorIndex];

                float4 color = float4(PositivePow(col.rgb, 2.2), opacity * col.a);
                return color;
            }

            uint OcclusionDebugOverlayOffset(uint2 coord)
            {
                return OCCLUSIONCULLINGCOMMONCONFIG_DEBUG_PYRAMID_OFFSET + coord.x + _OccluderMipLayoutSizeX * coord.y;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint2 coord = uint2(input.uv);
                uint subviewIndex = unity_StereoEyeIndex;

                uint total = 0;
                for (int i = 0; i < OCCLUSIONCULLINGCOMMONCONFIG_MAX_OCCLUDER_MIPS; ++i)
                {
                    int4 mipBounds = _OccluderMipBounds[i];
                    mipBounds.y += subviewIndex * _OccluderMipLayoutSizeY;

                    uint2 debugCoord = mipBounds.xy + uint2(min(int2(coord >> i), mipBounds.zw - 1));

                    total += _OcclusionDebugOverlay[OcclusionDebugOverlayOffset(debugCoord)];
                }

                if(total == 0)
                    return float4(0, 0, 0, 0);
 
                float cost = log2((float)total);

                uint screenTotal = _OcclusionDebugOverlay[0]; // This should be always >= 1, because total >= 1 at this point.

                float costScreenTotal = log2((float)screenTotal);
                return OverlayHeapMapColor(cost, costScreenTotal, 0.4);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
