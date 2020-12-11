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
            ZTest  Always // ignore depth buffer
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            RWStructuredBuffer<uint> _TileEntityMasks;

            struct Attributes
            {
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            struct Outputs
            {
                float4 unused : SV_Target0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(0.0 * 2.0 - 1.0, 0, 1, 0.0);

                return output;
            }

            Outputs Frag(Varyings input)
            {

                Outputs outputs;
                outputs.unused = float4(0.0, 0.0, 0.0, 0.0);
                return outputs;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
