Shader "Hidden/Test/BlitterPropertyBlock"
{
    SubShader
    {
        Pass
        {
            Name "Bilinear"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBilinear

            #define DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            ENDHLSL
        }
    }
}
