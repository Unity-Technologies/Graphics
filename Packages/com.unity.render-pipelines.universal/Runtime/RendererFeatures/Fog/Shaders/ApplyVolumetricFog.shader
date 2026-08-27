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
            // This pass only draws while one of the fog modes is active, so there is no off variant.
            #pragma multi_compile_fragment _FOG_ANALYTIC _FOG_VOLUMETRIC
            #pragma multi_compile_fragment _ _EXPOSURE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VolumetricFog.hlsl"

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

#if defined(_FOG_VOLUMETRIC)
                float lastSliceDist = _VBufferLastSliceDist;

                // Map this pixel's depth to the V-buffer's logarithmic Z slice coordinate (w). Sky
                // pixels decode from w=1.0, the deepest slice; everything else encodes its linear eye
                // depth, matching the froxel-Z parameterization the voxelizer/compute write with.
                float linearEyeDepth = isSky ? DecodeLogarithmicDepthGeneralized(1.0, _DepthDecodingParams)
                                             : LinearEyeDepth(deviceDepth, _ZBufferParams);
                float w = VolumetricFogSliceCoord(linearEyeDepth);

                // V-buffer reconstruction
                float4 vbufferSample;
                if (_VolumetricFilteringEnabled != 0.0)
                {
                    // The Gaussian spatial filter already ran this frame, so a single tap is enough.
                    vbufferSample = SampleVBufferTrilinear(uv, w);
                }
                else
                {
                    // A 4-tap biquadratic (B-spline) reconstruction hides the 8x8 blocks the jitter
                    // would otherwise show.
                    float2 xy = uv * _VBufferSize.xy;
                    float2 ic = floor(xy);
                    float2 fc = frac(xy);

                    float2 bqWeights[2], bqOffsets[2];
                    BiquadraticFilter(1.0 - fc, bqWeights, bqOffsets); // Inverse-translate the filter centered around 0.5

                    float2 texUv0 = (ic + float2(bqOffsets[0].x, bqOffsets[0].y)) * _VBufferSize.zw;
                    float2 texUv1 = (ic + float2(bqOffsets[1].x, bqOffsets[0].y)) * _VBufferSize.zw;
                    float2 texUv2 = (ic + float2(bqOffsets[0].x, bqOffsets[1].y)) * _VBufferSize.zw;
                    float2 texUv3 = (ic + float2(bqOffsets[1].x, bqOffsets[1].y)) * _VBufferSize.zw;

                    vbufferSample = (bqWeights[0].x * bqWeights[0].y) * SampleVBufferTrilinear(texUv0, w)
                                  + (bqWeights[1].x * bqWeights[0].y) * SampleVBufferTrilinear(texUv1, w)
                                  + (bqWeights[0].x * bqWeights[1].y) * SampleVBufferTrilinear(texUv2, w)
                                  + (bqWeights[1].x * bqWeights[1].y) * SampleVBufferTrilinear(texUv3, w);
                }

                float4 fogSample = DelinearizeRGBD(vbufferSample);
                float3 fogRadiance  = fogSample.rgb;
                float  opacity = OpacityFromOpticalDepth(fogSample.a);
#else
                // No V-buffer this frame: the analytic fog starts at the camera and covers the whole ray.
                float3 fogRadiance = 0.0;
                float  opacity = 0.0;
                float  lastSliceDist = 0.0;
#endif

                // Analytic fog fallback beyond the V-buffer (or the whole ray in analytic-only mode).
                ApplyAnalyticFog(rayDirWS.y, _WorldSpaceCameraPos.y, lastSliceDist, tFrag, fogRadiance, opacity);

                // The fog stack accumulates unexposed radiance, but this blends onto the
                // pre-exposed color target.
                fogRadiance *= GetPreExposureMultiplier();

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
