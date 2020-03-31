Shader "Hidden/ScriptableRenderPipeline/DebugDisplayProbeVolume"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"

        #define DEBUG_DISPLAY
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

        float3  _TextureViewScale;
        float3  _TextureViewBias;
        float3  _TextureViewResolution;
        float2  _ValidRange;
        int _ProbeVolumeAtlasSliceMode;
        // float   _RcpGlobalScaleFactor;
        SamplerState ltc_linear_clamp_sampler;
        TEXTURE3D(_AtlasTextureSH);

        TEXTURE2D(_AtlasTextureOctahedralDepth);
        float4 _AtlasTextureOctahedralDepthScaleBias;

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
            Name "ProbeVolume"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
            #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED

                // Layout Z slices horizontally in debug view UV space.
                float3 uvw;
                uvw.z = input.texcoord.x * _TextureViewResolution.z;
                uvw.x = frac(uvw.z);
                uvw.z = (floor(uvw.z) + 0.5f) / _TextureViewResolution.z;
                uvw.y = input.texcoord.y;

                // uvw is now in [0, 1] space.
                // Convert to specific view section of atlas.
                uvw = uvw * _TextureViewScale + _TextureViewBias;

                float4 valueShAr = saturate((SAMPLE_TEXTURE3D_LOD(_AtlasTextureSH, ltc_linear_clamp_sampler, float3(uvw.x, uvw.y, uvw.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) - _ValidRange.x) * _ValidRange.y);
                float4 valueShAg = saturate((SAMPLE_TEXTURE3D_LOD(_AtlasTextureSH, ltc_linear_clamp_sampler, float3(uvw.x, uvw.y, uvw.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) - _ValidRange.x) * _ValidRange.y);
                float4 valueShAb = saturate((SAMPLE_TEXTURE3D_LOD(_AtlasTextureSH, ltc_linear_clamp_sampler, float3(uvw.x, uvw.y, uvw.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) - _ValidRange.x) * _ValidRange.y);
                float valueValidity = saturate((SAMPLE_TEXTURE3D_LOD(_AtlasTextureSH, ltc_linear_clamp_sampler, float3(uvw.x, uvw.y, uvw.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 3), 0).x - _ValidRange.x) * _ValidRange.y);
                float2 valueOctahedralDepthMeanAndVariance = saturate((SAMPLE_TEXTURE2D_LOD(_AtlasTextureOctahedralDepth, ltc_linear_clamp_sampler, input.texcoord * _AtlasTextureOctahedralDepthScaleBias.xy + _AtlasTextureOctahedralDepthScaleBias.zw, 0).xy - _ValidRange.x) * _ValidRange.y);

                switch (_ProbeVolumeAtlasSliceMode)
                {
                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH00:
                    {

                        return float4(valueShAr.x, valueShAg.x, valueShAb.x, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH1_1:
                    {
                        return float4(valueShAr.y, valueShAg.y, valueShAb.y, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH10:
                    {
                        return float4(valueShAr.z, valueShAg.z, valueShAb.z, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH11:
                    {
                        return float4(valueShAr.w, valueShAg.w, valueShAb.w, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_VALIDITY:
                    {
                        return float4(lerp(float3(1, 0, 0), float3(0, 1, 0), valueValidity), 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_OCTAHEDRAL_DEPTH:
                    {
                        // Tonemap variance with sqrt() to bring it into a more similar scale to mean to make it more readable.
                        return float4(
                            valueOctahedralDepthMeanAndVariance.x,
                            (valueOctahedralDepthMeanAndVariance.y > 0.0f) ? sqrt(valueOctahedralDepthMeanAndVariance.y) : 0.0f,
                            0.0f,
                            1.0f
                        );
                    }

                    default: return float4(0.0, 0.0, 0.0, 1.0);
                }

            #else
                return float4(0.0, 0.0, 0.0, 1.0);
            #endif
            }

            ENDHLSL
        }

    }
    Fallback Off
}
