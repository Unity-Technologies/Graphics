Shader "CoreResources/FilterAreaLightCookies"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D( _texSource );
        uniform uint    _sourceMipLevel;
        uniform float4  _sourceSize;
        uniform float4  _targetSize;

        SAMPLER(sampler_LinearClamp);

        static const float  DELTA_SCALE = 1.0;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetQuadVertexPosition(input.vertexID);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            return output;
        }

        static const float4 KERNEL_WEIGHTS = float4( 0.00390625, 0.10937500, 0.21875000, 0.27343750 ) / 0.9375;
//        static const float4 KERNEL_WEIGHTS = float4( 0.006, 0.061, 0.242, 0.383 );

    ENDHLSL

    SubShader
    {
        // Simple copy to mip 0
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment frag

            float4  frag(Varyings input) : SV_Target
            {
                float2  UV = input.positionCS.xy * _targetSize.zw;
                return SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, 0.0 );
            }

            ENDHLSL
        }

        // 1: Horizontal Gaussian
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment frag

                float4 frag(Varyings input) : SV_Target
                {
                    float2  sourcePixelIndex = float2( 2.0 * input.positionCS.x, input.positionCS.y );
                    float2  UV = sourcePixelIndex * _sourceSize.zw;
                    float   delta = DELTA_SCALE * _sourceSize.z;
                            UV.x -= 3.0 * delta;

                    float4  sum  = KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.w * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta; // Center pixel
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.x += delta;
                    return sum;
                }

            ENDHLSL
        }

        // 2: Vertical Gaussian
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment frag

                float4 frag(Varyings input) : SV_Target
                {
                    float2  sourcePixelIndex = float2( input.positionCS.x, 2.0 * input.positionCS.y );
                    float2  UV = sourcePixelIndex * _sourceSize.zw;
                    float   delta = DELTA_SCALE * _sourceSize.w;
                            UV.y -= 3.0 * delta;

                    float4  sum  = KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.w * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta; // Center pixel
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _texSource, sampler_LinearClamp, UV, _sourceMipLevel ); UV.y += delta;
                    return sum;
                }
            ENDHLSL
        }
    }

    Fallback Off
}
