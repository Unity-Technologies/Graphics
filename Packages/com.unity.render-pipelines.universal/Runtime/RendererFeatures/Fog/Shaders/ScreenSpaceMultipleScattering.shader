// Screen-space approximation of multiple scattering through the fog: where opacity is high, the
// post-fog scene color is sampled from a Gaussian color pyramid at a higher mip level, which
// reads as "light scattered laterally through the medium".
//
// LOD = opacity * intensity. The trilinear sample interpolates between adjacent mips, so the
// transition between sharp (no fog) and blurry (dense fog) is continuous.
Shader "Hidden/Universal Render Pipeline/ScreenSpaceMultipleScattering"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Volumetric Fog SSMS"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

            #pragma multi_compile_local _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_ColorPyramidTexture);
            TEXTURE2D_X(_OpticalFogOpacity);

            float _MultipleScatteringIntensity;

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float opacity = saturate(SAMPLE_TEXTURE2D_X_LOD(_OpticalFogOpacity, sampler_LinearClamp, uv, 0).x);
                float lod = opacity * _MultipleScatteringIntensity;
                return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, sampler_TrilinearClamp, uv, lod);
            }
            ENDHLSL
        }
    }
}
