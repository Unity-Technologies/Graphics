Shader "Hidden/HDRP/CullingRasterizer"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Pass 0 - Shader Variants (tiling)"

            // Stencil is used to skip background/sky pixels.
            Stencil
            {
                ReadMask[_StencilMask]
                Ref  [_StencilRef]
                Comp [_StencilCmp]
                Pass Keep
            }

            ZWrite Off
            ZTest LEqual
            Blend Off
        // TODO this is not good, we need pixel perfect here too expensive
            Cull Off // Don't cull so the raster work when the camera is both inside and outside the bounds without the need to change renderstate

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

//#pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #define FINE_BINNING // This tile raster is for fine binning

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/TilingAndBinningUtilities.hlsl"

#if defined(PLATFORM_NEEDS_UNORM_UAV_SPECIFIER) && defined(PLATFORM_SUPPORTS_EXPLICIT_BINDING)
            // Explicit binding is needed on D3D since we bind the UAV to slot 1 and we don't have a colour RT bound to fix a D3D warning.
            RWStructuredBuffer<uint> _TileEntityMasks : register(u1);
#else
            RWStructuredBuffer<uint> _TileEntityMasks;
#endif

            int _Category;
            int _LightIndex;
            float3 _Range;
            float3 _Offset;

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct Outputs
            {
                float4 unused : SV_Target0;
            };

            Varyings Vert(Attributes att, uint vertID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(att);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                
                // Our light are already in camera relative space, so don't apply it again
                float3 positionRWS = mul(GetRawUnityObjectToWorld(), float4(att.positionOS * _Range + _Offset, 1.0)).xyz;

                //float3 positionRWS = TransformObjectToWorld(att.positionOS);
                // Compute the clip space position
                output.positionCS = TransformWorldToHClip(positionRWS);

                return output;
            }

            // TODO test woth DXC...
#if defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS)
            uint WaveCompactValue(uint checkValue)
            {
                uint mask; // lane unique compaction mask
                while (true) // Loop until all active lanes removed
                {
                    uint firstValue = WaveReadFirstLane(checkValue);
                    mask = WaveBallot(firstValue == checkValue); // mask is only updated for remaining active lanes
                    if (firstValue == checkValue)
                        break; // exclude all lanes with firstValue from next iteration
                }
                // At this point, each lane of mask should contain a bit mask of all other lanes with the same value.
                uint index = WavePrefixSum(mask); // Note this is performed independently on a different mask for each lane.
                return index;
            }
#endif


            void Frag(Varyings varying, out float4 color : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varying);
                color = float4(0.0, 0.0, 0.0, 0.0); // not used

                uint tile = ComputeTileIndex(varying.positionCS.xy);
                uint lightIndex = _LightIndex; // Be sure index is < max (on CPU)
                const uint lightBit = 1 << (lightIndex % 32); // find correct light bit
                const uint word = lightIndex / 32;

                uint tileBufferHeaderIndex = ComputeTileBufferHeaderIndex(tile, _Category, unity_StereoEyeIndex, FINE_TILE_BUFFER_DIMS);
                const uint wordIndex = (tileBufferHeaderIndex * MAX_WORD_PER_ENTITY) + word; // find correct word
#if defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS)
                const uint key = (wordIndex << 9); // FRUSTUM_GRID_MAX_LIGHTS_LOG2 (log2(512 => 9)

                [branch]
                const uint hash = WaveCompactValue(key...);
                if (hash == 0) // Branch only for first occurrence of unique key within wavefront
#endif
                    InterlockedOr(_TileEntityMasks[wordIndex], lightBit);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
