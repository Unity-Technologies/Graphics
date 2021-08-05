Shader "Hidden/PostProcessing/SubpixelMorphologicalAntialiasing"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 4
        [HideInInspector] _StencilMask("_StencilMask", Int) = 4
    }

    HLSLINCLUDE

#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
#pragma multi_compile_local SMAA_PRESET_LOW SMAA_PRESET_MEDIUM SMAA_PRESET_HIGH

        ENDHLSL

        SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Edge detection 
        Pass
        {
            Stencil
            {
                WriteMask [_StencilMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

                #pragma vertex VertEdge
                #pragma fragment FragEdge
                #include "SubpixelMorphologicalAntialiasingBridge.hlsl"

            ENDHLSL
        }

        // Blend Weights Calculation
        Pass
        {
            Stencil
            {
                WriteMask[_StencilMask]
                ReadMask [_StencilMask]
                Ref [_StencilRef]
                Comp Equal
                Pass Replace
            }

            HLSLPROGRAM

                #pragma vertex VertBlend
                #pragma fragment FragBlend
                #include "SubpixelMorphologicalAntialiasingBridge.hlsl"

            ENDHLSL
        }

        // Neighborhood Blending
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertNeighbor
                #pragma fragment FragNeighbor
                #include "SubpixelMorphologicalAntialiasingBridge.hlsl"

            ENDHLSL
        }
    }
}
