Shader "Hidden/HDRP/CullingRasterizer"
{
    Properties
    {
       _LightIndex("LightIndex", Float) = 0.0
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
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

//#pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

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

            float _LightIndex;

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
                float3 positionRWS = mul(GetRawUnityObjectToWorld(), float4(att.positionOS, 1.0)).xyz;

                //float3 positionRWS = TransformObjectToWorld(att.positionOS);
                // Compute the clip space position
                output.positionCS = TransformWorldToHClip(positionRWS);

                return output;
            }

            void Frag(Varyings varying, out float4 color : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varying);
                color = float4(0.0, 0.0, 0.0, 0.0); // not used

                uint tile = ComputeTileIndex(varying.positionCS.xy);
                uint lightIndex = _LightIndex; // Be sure index is < max (on CPU)
                const uint lightBit = 1 << (lightIndex % 32); // find correct light bit
                const uint word = lightIndex / 32;

                uint tileBufferHeaderIndex = ComputeTileBufferHeaderIndex(tile, BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT, unity_StereoEyeIndex, FINE_TILE_BUFFER_DIMS);
                const uint wordIndex = (tileBufferHeaderIndex * MAX_WORD_PER_ENTITY) + word; // find correct word

                InterlockedOr(_TileEntityMasks[wordIndex], lightBit);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
