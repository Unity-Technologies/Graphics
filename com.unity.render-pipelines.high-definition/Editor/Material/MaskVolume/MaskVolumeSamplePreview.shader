Shader "Hidden/Debug/MaskVolumeSamplePreview"
{
    Properties
    {
        _Exposure("_Exposure", Range(-10.0,10.0)) = 0.0
        _MaskVolumeResolution("_MaskVolumeResolution", Vector) = (0, 0, 0, 0)
        _MaskVolumeProbeDisplayRadiusWS("_MaskVolumeProbeDisplayRadiusWS", Float) = 1.0
        _MaskVolumeAtlasBiasTexels("_MaskVolumeAtlasBiasTexels", Vector) = (0, 0, 0, 0)
        _MaskVolumeIsResidentInAtlas("_MaskVolumeIsResidentInAtlas", Float) = 0.0
        _MaskVolumeDrawWeightThresholdSquared("_MaskVolumeDrawWeightThresholdSquared", Float) = 0.0
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

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeAtlas.hlsl"

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

            float3 _MaskVolumeResolution;
            float4x4 _ProbeIndex3DToPositionWSMatrix;
            float _MaskVolumeProbeDisplayRadiusWS;
            float3 _MaskVolumeAtlasBiasTexels;
            int _MaskVolumeIsResidentInAtlas;
            float _MaskVolumeDrawWeightThresholdSquared;

            uint3 ComputeWriteIndexFromReadIndex(uint readIndex, float3 resolution)
            {
                // _MaskVolumeAtlasReadBuffer[z * resolutionY * resolutionX + y * resolutionX + x]
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
                vertexPositionOS *= _MaskVolumeProbeDisplayRadiusWS;

                o.probeIndex3D = ComputeWriteIndexFromReadIndex(probeIndex1D, _MaskVolumeResolution);
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

            float3 SampleProbeMaskData(int3 probeIndexAtlas3D, float3 normalWS)
            {
                float3 mask = 0.0;

                MaskVolumeData coefficients;
                ZERO_INITIALIZE(MaskVolumeData, coefficients);
                MaskVolumeLoadAccumulate(probeIndexAtlas3D, 1.0f, coefficients);
                mask = coefficients.data[0].rgb;

                return mask;
            }

            float3 SampleMaskData(int3 probeIndexAtlas3D, float3 normalWS)
            {
                float3 mask = SampleProbeMaskData(probeIndexAtlas3D, normalWS);
                mask = max(0.0, mask);
                return mask;
            }

            void ClipDrawWeightThreshold(float3 mask)
            {
                clip((dot(mask, mask) >= (_MaskVolumeDrawWeightThresholdSquared)) ? 1.0 : -1.0);
            }

            float4 frag(v2f i) : SV_Target
            {
                ClipProbeSphere(i.uv);
                float3 normalWS = ComputeProbeNormalWSFromCameraFacingOrtho(i.uv);
                
                float3 mask = 0.0;

                if (_MaskVolumeIsResidentInAtlas)
                {
                    // Due to probeIndex3D getting stored as a float and interpolated, we need to round before converting to int.
                    // Otherwise our texel coordinate will oscillate between probes randomly (based on precision).
                    int3 probeIndexAtlas3D = (int3)(i.probeIndex3D + 0.5 + _MaskVolumeAtlasBiasTexels);

                    mask = SampleMaskData(probeIndexAtlas3D, normalWS);
                }

                ClipDrawWeightThreshold(mask);

                mask *= exp2(_Exposure) * GetCurrentExposureMultiplier();

                return float4(mask, 1.0);
            }
            ENDHLSL
        }
    }
}
