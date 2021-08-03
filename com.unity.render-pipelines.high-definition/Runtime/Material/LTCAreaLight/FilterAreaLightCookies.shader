Shader "Hidden/CoreResources/FilterAreaLightCookies"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma editor_sync_compilation

        #pragma vertex Vert
        #pragma fragment frag

        // SRP includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        // Input Data
        TEXTURE2D( _SourceTexture );
        TEXTURECUBE( _SourceCubeTexture );
        SAMPLER( sampler_SourceCubeTexture );
        uniform uint    _SourceMipLevel;
        uniform float4  _SourceSize;
        uniform float4  _UVLimits;

        // Shared constants
        static const float  DELTA_SCALE = 1.0;
        static const float4 KERNEL_WEIGHTS = float4( 0.00390625, 0.10937500, 0.21875000, 0.27343750 ) / 0.9375;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
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

        float2 ClampUV(float2 uv)
        {
            // Clamp UVs to source size minus half pixel
            return clamp(uv, _UVLimits.xy, _UVLimits.zw - _SourceSize.zw * 0.5);
        }

    ENDHLSL

    SubShader
    {
        // Simple copy to mip 0
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            float4 frag(Varyings input) : SV_Target
            {
                float2  UV = input.texcoord;

                // Since we blit the cookie texture into a common texture, to avoid leaking we blit with an extra border
                if (any(input.positionCS.xy > _SourceSize.xy))
                    return 0.0f;

                // Because the viewport is bigger than the source texture, we need to rescale the UVs.
                // The scale is (viewportSize / SourceSize)
                float2 uvScale = _SourceSize.zw;
                return SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, UV * uvScale, 0);

            }
            ENDHLSL
        }

        // 1: Horizontal Gaussian
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                float4 frag(Varyings input) : SV_Target
                {
                    float2  UV = float2(input.texcoord.x, input.texcoord.y) * _SourceSize.xy;
                    float   delta = DELTA_SCALE * _SourceSize.z;
                            UV.x -= 3.0 * delta;

                    float4  sum  = KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.w * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta; // Center pixel
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta;
                            sum += KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.x += delta;
                    return sum;
                }

            ENDHLSL
        }

        // 2: Vertical Gaussian
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                float4 frag(Varyings input) : SV_Target
                {
                    float2  UV = float2(input.texcoord.x, input.texcoord.y) * _SourceSize.xy;
                    float   delta = DELTA_SCALE * _SourceSize.w;
                            UV.y -= 3.0 * delta;

                    float4  sum  = KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.w * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta; // Center pixel
                            sum += KERNEL_WEIGHTS.z * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.y * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta;
                            sum += KERNEL_WEIGHTS.x * SAMPLE_TEXTURE2D_LOD( _SourceTexture, s_linear_clamp_sampler, ClampUV(UV), _SourceMipLevel ); UV.y += delta;
                    return sum;
                }
            ENDHLSL
        }

        // 3. Project Cube to Octahedral to mip 0
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            float4 frag(Varyings input) : SV_Target
            {
                float2 UV = saturate(input.texcoord);
                float3 dir = UnpackNormalOctQuadEncode(2.0f*UV - 1.0f);
                return float4(SAMPLE_TEXTURECUBE_LOD(_SourceCubeTexture, sampler_SourceCubeTexture, dir, 0).rgb, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
