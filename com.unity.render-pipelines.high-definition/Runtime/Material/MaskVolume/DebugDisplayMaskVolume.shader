Shader "Hidden/ScriptableRenderPipeline/DebugDisplayMaskVolume"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeAtlas.hlsl"

        #define DEBUG_DISPLAY
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

        float3  _TextureViewScale;
        float3  _TextureViewBias;
        float3  _TextureViewResolution;

        struct Attributes
        {
            uint vertexID : VERTEXID_SEMANTIC;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

            return output;
        }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "MaskVolume"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                // Layout Z slices horizontally in debug view UV space.
                float3 uvw;
                uvw.z = input.texcoord.x * _TextureViewResolution.z;
                uvw.x = frac(uvw.z);
                uvw.z = (floor(uvw.z) + 0.5f) / _TextureViewResolution.z;
                uvw.y = input.texcoord.y;

                // uvw is now in [0, 1] space.
                // Convert to specific view section of atlas.
                uvw = uvw * _TextureViewScale + _TextureViewBias;

                MaskVolumeData coefficients;
                ZERO_INITIALIZE(MaskVolumeData, coefficients);
                MaskVolumeSampleAccumulate(uvw, 1.0f, coefficients);
                return float4(coefficients.data[0].rgb, 1.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
