Shader "Hidden/Debug/ProbeVolumeSHPreview"
{
    Properties
    {
        _Exposure("_Exposure", Range(-10.0,10.0)) = 0.0
        _ProbeVolumeResolution("_ProbeVolumeResolution", Vector) = (0, 0, 0, 0)
        _ProbeVolumeProbeDisplayRadiusWS("_ProbeVolumeProbeDisplayRadiusWS", Float) = 1.0
        _ProbeVolumeAtlasBiasTexels("_ProbeVolumeAtlasBiasTexels", Vector) = (0, 0, 0, 0)
        _ProbeVolumeIsResidentInAtlas("_ProbeVolumeIsResidentInAtlas", Float) = 0.0
        _ProbeVolumeHighlightNegativeRinging("_ProbeVolumeHighlightNegativeRinging", Float) = 0.0
        _ProbeVolumeDrawValidity("_ProbeVolumeDrawValidity", Float) = 0.0
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "Transparent" }
        ZWrite On
        Cull Front

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "Forward" }

            HLSLPROGRAM

            #pragma editor_sync_compilation

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L0 PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L1 PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L2

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAtlas.hlsl"
#endif

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 probeIndex3D : TEXCOORD1;
            };

            float _Exposure;

            float3 _ProbeVolumeResolution;
            float4x4 _ProbeIndex3DToPositionWSMatrix;
            float _ProbeVolumeProbeDisplayRadiusWS;
            float3 _ProbeVolumeAtlasBiasTexels;
            int _ProbeVolumeIsResidentInAtlas;
            int _ProbeVolumeHighlightNegativeRinging;
            int _ProbeVolumeDrawValidity;

            uint3 ComputeWriteIndexFromReadIndex(uint readIndex, float3 resolution)
            {
                // _ProbeVolumeAtlasReadBuffer[z * resolutionY * resolutionX + y * resolutionX + x]
                // TODO: Could implement as floating point operations, which is likely faster.
                // Would need to verify precision.
                uint x = readIndex % (uint)resolution.x;
                uint y = (readIndex / (uint)resolution.x) % (uint)resolution.y;
                uint z = readIndex / ((uint)resolution.y * (uint)resolution.x);

                return uint3(x, y, z);
            }

            v2f vert(appdata v)
            {
                v2f o;

                uint probeIndex1D = v.vertexID / 6u;
                uint probeTriangleIndex = (v.vertexID / 3u) & 1u;
                uint probeVertexIndex = v.vertexID - probeIndex1D * 6u - probeTriangleIndex * 3u;

                float2 vertexPositionOS = (probeTriangleIndex == 1u)
                    ? float2((probeVertexIndex & 1u), saturate(probeVertexIndex))
                    : float2(saturate(probeVertexIndex), saturate((float)probeVertexIndex - 1.0));
                o.uv = vertexPositionOS;
                vertexPositionOS = vertexPositionOS * 2.0 - 1.0;
                vertexPositionOS *= _ProbeVolumeProbeDisplayRadiusWS;

                o.probeIndex3D = ComputeWriteIndexFromReadIndex(probeIndex1D, _ProbeVolumeResolution);
                float3 probeOriginWS = mul(_ProbeIndex3DToPositionWSMatrix, float4(o.probeIndex3D, 1.0)).xyz;
                float3 probeOriginRWS = GetCameraRelativePositionWS(probeOriginWS);
                
                float3 cameraRightWS = mul(float4(1.0, 0.0, 0.0, 0.0), UNITY_MATRIX_V).xyz;
                float3 cameraUpWS = mul(float4(0.0, 1.0, 0.0, 0.0), UNITY_MATRIX_V).xyz;

                float3 positionRWS = (cameraRightWS * vertexPositionOS.x + cameraUpWS * vertexPositionOS.y) + probeOriginRWS;
                o.positionCS = TransformWorldToHClip(positionRWS);

                return o;
            }

            void ClipProbeSphere(float2 uv)
            {
                float2 positionProbeCard = uv * 2.0 - 1.0;
                clip(dot(positionProbeCard, positionProbeCard) < 1.0 ? 1.0 : -1.0);
            }

            float3 ComputeProbeNormalWSFromCameraFacingOrtho(float2 uv)
            {
                // Reconstruct a surface normal vector for our virtual probe sphere using the knowledge that
                // our card is camera aligned, and we can project from the 2D disc coordinate to 3D sphere surface coordinate.
                // This will not take into account perspective - but as a method to preview probe SH data, this limitation fine visually.
                float2 normalOSXY = uv * 2.0 - 1.0;
                float normalOSZ = (1.0 - dot(normalOSXY, normalOSXY));

                float3 normalWS = mul(float4(normalOSXY, normalOSZ, 0.0), UNITY_MATRIX_V).xyz;
                return normalWS;
            }

            float3 SampleProbeOutgoingRadiance(int3 probeIndexAtlas3D, float3 normalWS)
            {
                float3 outgoingRadiance = 0.0;

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED

#if defined(PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L0)
                ProbeVolumeSphericalHarmonicsL0 coefficients;
                ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL0, coefficients);
                ProbeVolumeLoadAccumulateSphericalHarmonicsL0(probeIndexAtlas3D, 1.0f, coefficients);
                ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL0(coefficients);
                outgoingRadiance = coefficients.data[0].xyz;

#elif defined(PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L1)
                ProbeVolumeSphericalHarmonicsL1 coefficients;
                ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL1, coefficients);
                ProbeVolumeLoadAccumulateSphericalHarmonicsL1(probeIndexAtlas3D, 1.0f, coefficients);
                ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL1(coefficients);
                outgoingRadiance = SHEvalLinearL0L1(normalWS, coefficients.data[0], coefficients.data[1], coefficients.data[2]);

#elif defined(PROBE_VOLUMES_ENCODING_SPHERICAL_HARMONICS_L2)
                ProbeVolumeSphericalHarmonicsL2 coefficients;
                ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL2, coefficients);
                ProbeVolumeLoadAccumulateSphericalHarmonicsL2(probeIndexAtlas3D, 1.0f, coefficients);
                ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL2(coefficients);
                outgoingRadiance = SampleSH9(coefficients.data, normalWS);
#endif

#endif 
                return outgoingRadiance;
            }

            float3 SampleProbeValidity(int3 probeIndexAtlas3D)
            {
                float3 color = 0.0;

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED
                float validity = ProbeVolumeLoadValidity(probeIndexAtlas3D);
                color = lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
#endif
                
                return color;
            }

            float ComputeBlink(float time, float frequency)
            {
                return sin(time * 2.0 * PI * frequency - 0.5 * PI) * 0.5 + 0.5;
            }

            struct ProbeSampleData
            {
                float3 color;
                bool needsExposure;
            };

            ProbeSampleData SampleProbeData(int3 probeIndexAtlas3D, float3 normalWS)
            {
                ProbeSampleData probeSampleData;
                ZERO_INITIALIZE(ProbeSampleData, probeSampleData);

                if (_ProbeVolumeDrawValidity)
                {
                    probeSampleData.color = SampleProbeValidity(probeIndexAtlas3D);
                    probeSampleData.needsExposure = false;
                }
                else
                {
                    float3 outgoingRadiance = SampleProbeOutgoingRadiance(probeIndexAtlas3D, normalWS);

                    outgoingRadiance = _ProbeVolumeHighlightNegativeRinging
                        ? (any(outgoingRadiance < 0.0) ? float3(ComputeBlink(_Time.y, 1.0) * 0.75 + 0.25, 0.0, 0.0) : outgoingRadiance)
                        : outgoingRadiance; 

                    outgoingRadiance = max(0.0, outgoingRadiance);
                    probeSampleData.color = outgoingRadiance;
                    probeSampleData.needsExposure = true;
                }

                return probeSampleData;
            }

            float4 frag(v2f i) : SV_Target
            {
                ClipProbeSphere(i.uv);
                float3 normalWS = ComputeProbeNormalWSFromCameraFacingOrtho(i.uv);
                
                ProbeSampleData probeSampleData;
                ZERO_INITIALIZE(ProbeSampleData, probeSampleData);

                if (_ProbeVolumeIsResidentInAtlas)
                {
                    // Due to probeIndex3D getting stored as a float and interpolated, we need to round before converting to int.
                    // Otherwise our texel coordinate will oscillate between probes randomly (based on precision).
                    int3 probeIndexAtlas3D = (int3)(i.probeIndex3D + 0.5 + _ProbeVolumeAtlasBiasTexels);

                    probeSampleData = SampleProbeData(probeIndexAtlas3D, normalWS);
                }

                probeSampleData.color *= probeSampleData.needsExposure
                    ? (exp2(_Exposure) * GetCurrentExposureMultiplier())
                    : 1.0;

                return float4(probeSampleData.color, 1.0);
            }
            ENDHLSL
        }
    }
}
