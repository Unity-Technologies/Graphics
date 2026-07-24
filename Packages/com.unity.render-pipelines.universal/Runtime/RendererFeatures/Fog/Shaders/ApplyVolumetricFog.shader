Shader "Hidden/Universal Render Pipeline/ApplyVolumetricFog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Apply Volumetric Fog"
            ZWrite Off
            ZTest Always
            Cull Off
            // Alpha-blend color, overwrite opacity
            Blend 0 One OneMinusSrcAlpha, Zero One

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 4.5

            // Enabled when the screen-space multiple-scattering pass is going to run afterwards.
            #pragma multi_compile_local _ _WRITE_FOG_OPACITY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VolumetricFog.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE3D(_VBuffer);

            float4 _DepthEncodingParams;
            float4 _DepthDecodingParams;
            float4 _VBufferSize;
            float  _VBufferRcpSliceCount;
            float  _VBufferLastSliceDist;       // Distance at the center of the last slice (along ray).
            float4 _FogColor;                   // RGB used, A unused
            float  _MaxFogDistance;             // Sky pixels are fogged as if at this along-ray distance.
            float  _VolumetricFilteringEnabled; // 1 when the Gaussian spatial filter ran this frame.

            struct FragOutput
            {
                float4 color : SV_Target0;
            #if defined(_WRITE_FOG_OPACITY)
                float opacity : SV_Target1;
            #endif
            };

            FragOutput Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float deviceDepth = LoadSceneDepth(uint2(input.positionCS.xy));
                bool isSky = deviceDepth == UNITY_RAW_FAR_CLIP_VALUE;

                // Reconstruct the world-space ray for this pixel.
                float3 worldPos = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 toFragment = worldPos - _WorldSpaceCameraPos.xyz;
                float3 rayDirWS = normalize(toFragment);

                // For non-sky pixels we want the actual along-ray distance to the surface; for sky we
                // use _MaxFogDistance.
                float tFrag = isSky ? _MaxFogDistance : length(toFragment);

                // Map this pixel's depth to the V-buffer's logarithmic Z slice coordinate (w). Sky
                // pixels decode from w=1.0, the deepest slice; everything else encodes its linear eye
                // depth, matching the froxel-Z parameterization the voxelizer/compute write with.
                float linearEyeDepth = isSky ? DecodeLogarithmicDepthGeneralized(1.0, _DepthDecodingParams)
                                             : LinearEyeDepth(deviceDepth, _ZBufferParams);
                float w = saturate(EncodeLogarithmicDepthGeneralized(linearEyeDepth, _DepthEncodingParams));

                // Bias the lookup toward the camera to prevent the trilinear filter from bleeding into
                // zeroed slices past the MaxZ early-out.
                w -= (sqrt(3.0) / 2.0) * _VBufferRcpSliceCount;

                // V-buffer reconstruction
                float4 vbufferSample;
                if (_VolumetricFilteringEnabled != 0.0)
                {
                    // The Gaussian spatial filter already ran this frame, so a single trilinear tap
                    // (bilinear XY × linear Z) is enough.
                    vbufferSample = SAMPLE_TEXTURE3D_LOD(_VBuffer, sampler_LinearClamp, float3(uv, w), 0);
                }
                else
                {
                    // Do a 4-tap biquadratic (B-spline) reconstruction that hides the 8×8 blocks
                    // the jitter would otherwise show.
                    float2 xy = uv * _VBufferSize.xy;
                    float2 ic = floor(xy);
                    float2 fc = frac(xy);

                    float2 bqWeights[2], bqOffsets[2];
                    BiquadraticFilter(1.0 - fc, bqWeights, bqOffsets); // Inverse-translate the filter centered around 0.5

                    float2 texUv0 = (ic + float2(bqOffsets[0].x, bqOffsets[0].y)) * _VBufferSize.zw;
                    float2 texUv1 = (ic + float2(bqOffsets[1].x, bqOffsets[0].y)) * _VBufferSize.zw;
                    float2 texUv2 = (ic + float2(bqOffsets[0].x, bqOffsets[1].y)) * _VBufferSize.zw;
                    float2 texUv3 = (ic + float2(bqOffsets[1].x, bqOffsets[1].y)) * _VBufferSize.zw;

                    vbufferSample = (bqWeights[0].x * bqWeights[0].y) * SAMPLE_TEXTURE3D_LOD(_VBuffer, sampler_LinearClamp, float3(texUv0, w), 0)
                                  + (bqWeights[1].x * bqWeights[0].y) * SAMPLE_TEXTURE3D_LOD(_VBuffer, sampler_LinearClamp, float3(texUv1, w), 0)
                                  + (bqWeights[0].x * bqWeights[1].y) * SAMPLE_TEXTURE3D_LOD(_VBuffer, sampler_LinearClamp, float3(texUv2, w), 0)
                                  + (bqWeights[1].x * bqWeights[1].y) * SAMPLE_TEXTURE3D_LOD(_VBuffer, sampler_LinearClamp, float3(texUv3, w), 0);
                }
                float4 fogData = DelinearizeRGBD(vbufferSample);
                float3 fogRadiance  = fogData.rgb;
                float  opticalDepth = fogData.a;
                float  opacity = OpacityFromOpticalDepth(opticalDepth);

                // Analytic height-fog fallback beyond the V-buffer.
                float distDelta = tFrag - _VBufferLastSliceDist;
                if (distDelta > 0.0)
                {
                    float cosZenith = rayDirWS.y;
                    float startHeight = _WorldSpaceCameraPos.y + _VBufferLastSliceDist * cosZenith;
                    float odFallback = OpticalDepthHeightFog(_VolumetricFogBaseExtinction, _VolumetricFogBaseHeight,
                                                             _VolumetricFogExponents, cosZenith, startHeight, distDelta);
                    float trFallback = TransmittanceFromOpticalDepth(odFallback);
                    float trCamera = 1.0 - opacity;
                    fogRadiance += trCamera * _FogColor.rgb * (1.0 - trFallback);
                    opacity = 1.0 - (trCamera * trFallback);
                }

                FragOutput o;
                o.color = float4(fogRadiance, opacity);
            #if defined(_WRITE_FOG_OPACITY)
                o.opacity = saturate(opacity);
            #endif
                return o;
            }
            ENDHLSL
        }
    }
}
