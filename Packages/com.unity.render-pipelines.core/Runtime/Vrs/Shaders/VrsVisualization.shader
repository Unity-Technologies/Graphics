Shader "Hidden/Core/VrsVisualization"
{
    SubShader
    {
        Pass
        {
            Name "VrsVisualization"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            //#pragma enable_d3d11_debug_symbols

            #pragma exclude_renderers glcore gles3

            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TYPED_TEXTURE2D(uint, _ShadingRateImage);
            StructuredBuffer<float4> _VisualizationLut;

            uniform float4 _VisualizationParams;

            #define PixelToTileScale _VisualizationParams.xy

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint2 pixel = (uint2)(input.positionCS.xy * PixelToTileScale);
                uint shadingRate = LOAD_TEXTURE2D_LOD(_ShadingRateImage, pixel, 0);

                return _VisualizationLut[shadingRate];
            }
            ENDHLSL
        }
    }

    Fallback Off
}
