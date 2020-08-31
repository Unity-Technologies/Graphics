Shader "Hidden/ScriptableRenderPipeline/DebugDisplayProbeVolume"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAtlas.hlsl"
        #endif

        #define DEBUG_DISPLAY
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

        float3  _TextureViewScale;
        float3  _TextureViewBias;
        float3  _TextureViewResolution;
        float2  _ValidRange;
        int _ProbeVolumeAtlasSliceMode;
        // float   _RcpGlobalScaleFactor;
        SamplerState ltc_linear_clamp_sampler;

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

            #if SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
                ProbeVolumeSphericalHarmonicsL1 coefficients;
                ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL1, coefficients);
                ProbeVolumeSampleAccumulateSphericalHarmonicsL1(uvw, 1.0f, coefficients);
                ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL1(coefficients);
                float4 valueShAr = saturate((coefficients.data[0] - _ValidRange.x) * _ValidRange.y);
                float4 valueShAg = saturate((coefficients.data[1] - _ValidRange.x) * _ValidRange.y);
                float4 valueShAb = saturate((coefficients.data[2] - _ValidRange.x) * _ValidRange.y);

                float4 valueShBr = 0.0f;
                float4 valueShBg = 0.0f;
                float4 valueShBb = 0.0f;
                float4 valueShC = 0.0f;

            #elif SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
                ProbeVolumeSphericalHarmonicsL2 coefficients;
                ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL2, coefficients);
                ProbeVolumeSampleAccumulateSphericalHarmonicsL2(uvw, 1.0f, coefficients);
                ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL2(coefficients);
                float4 valueShAr = saturate((coefficients.data[0] - _ValidRange.x) * _ValidRange.y);
                float4 valueShAg = saturate((coefficients.data[1] - _ValidRange.x) * _ValidRange.y);
                float4 valueShAb = saturate((coefficients.data[2] - _ValidRange.x) * _ValidRange.y);

                float4 valueShBr = saturate((coefficients.data[3] - _ValidRange.x) * _ValidRange.y);
                float4 valueShBg = saturate((coefficients.data[4] - _ValidRange.x) * _ValidRange.y);
                float4 valueShBb = saturate((coefficients.data[5] - _ValidRange.x) * _ValidRange.y);
                float4 valueShC = saturate((coefficients.data[6] - _ValidRange.x) * _ValidRange.y);

            #endif

                float valueValidity = saturate((ProbeVolumeSampleValidity(uvw) - _ValidRange.x) * _ValidRange.y);
                
            #if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
                float2 valueOctahedralDepthMeanAndVariance = saturate((SAMPLE_TEXTURE2D_LOD(_AtlasTextureOctahedralDepth, ltc_linear_clamp_sampler, input.texcoord * _AtlasTextureOctahedralDepthScaleBias.xy + _AtlasTextureOctahedralDepthScaleBias.zw, 0).xy - _ValidRange.x) * _ValidRange.y);
            #endif

                switch (_ProbeVolumeAtlasSliceMode)
                {
                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH00:
                    {

                        return float4(valueShAr.w, valueShAg.w, valueShAb.w, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH1_1:
                    {
                        return float4(valueShAr.x, valueShAg.x, valueShAb.x, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH10:
                    {
                        return float4(valueShAr.y, valueShAg.y, valueShAb.y, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH11:
                    {
                        return float4(valueShAr.z, valueShAg.z, valueShAb.z, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH2_2:
                    {
                        return float4(valueShBr.x, valueShBg.x, valueShBb.x, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH2_1:
                    {
                        return float4(valueShBr.y, valueShBg.y, valueShBb.y, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH20:
                    {
                        return float4(valueShBr.z, valueShBg.z, valueShBb.z, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH21:
                    {
                        return float4(valueShBr.w, valueShBg.w, valueShBb.w, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_IRRADIANCE_SH22:
                    {
                        return float4(valueShC.x, valueShC.y, valueShC.z, 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_VALIDITY:
                    {
                        return float4(lerp(float3(1, 0, 0), float3(0, 1, 0), valueValidity), 1);
                    }

                    case PROBEVOLUMEATLASSLICEMODE_OCTAHEDRAL_DEPTH:
                    {
                    #if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
                        // Tonemap variance with sqrt() to bring it into a more similar scale to mean to make it more readable.
                        return float4(
                            valueOctahedralDepthMeanAndVariance.x,
                            (valueOctahedralDepthMeanAndVariance.y > 0.0f) ? sqrt(valueOctahedralDepthMeanAndVariance.y) : 0.0f,
                            0.0f,
                            1.0f
                        );
                    #else
                        return float4(0.0f, 0.0f, 0.0f, 1.0f);
                    #endif
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
