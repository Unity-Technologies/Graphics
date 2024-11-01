Shader "Hidden/Universal Render Pipeline/StencilDitherMaskSeed"
{
    Properties
    {
        _StencilRefDitherMask ("StencilRefDitherMask", Int) = 0
        _StencilWriteDitherMask ("StencilWriteDitherMask", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "StencilDitherMaskSeed"

            // -------------------------------------
            // Render State Commands
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            // -------------------------------------
            // Stencil Settings
            Stencil
            {
                Ref [_StencilRefDitherMask]
                WriteMask [_StencilWriteDitherMask]
                CompFront Always
                PassFront Replace
                PassBack Replace
            }

            HLSLPROGRAM
            #pragma target 4.5

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vert
            #pragma fragment Pixel

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            uint _StencilDitherPattern;

            void Pixel(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint a = (uint)input.positionCS.x & 0x1;
                uint b = (((uint)input.positionCS.y & 0x1) ^ a) << 1;
                if ((a + b) != _StencilDitherPattern)
                    discard;
            }

            ENDHLSL
        }
    }
}
