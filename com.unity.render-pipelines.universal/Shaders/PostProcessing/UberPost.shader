Shader "Hidden/Universal Render Pipeline/UberPost"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_local_fragment _ _DISTORTION
        #pragma multi_compile_local_fragment _ _CHROMATIC_ABERRATION
        #pragma multi_compile_local_fragment _ _BLOOM_LQ _BLOOM_HQ _BLOOM_LQ_DIRT _BLOOM_HQ_DIRT
        #pragma multi_compile_local_fragment _ _HDR_GRADING _TONEMAP_ACES _TONEMAP_NEUTRAL
        #pragma multi_compile_local_fragment _ _FILM_GRAIN
        #pragma multi_compile_local_fragment _ _DITHERING
        #pragma multi_compile_local_fragment _ _LINEAR_TO_SRGB_CONVERSION
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        // Hardcoded dependencies to reduce the number of variants
        #if _BLOOM_LQ || _BLOOM_HQ || _BLOOM_LQ_DIRT || _BLOOM_HQ_DIRT
            #define BLOOM
            #if _BLOOM_LQ_DIRT || _BLOOM_HQ_DIRT
                #define BLOOM_DIRT
            #endif
        #endif

        TEXTURE2D_X(_SourceTex);
        TEXTURE2D_X(_Bloom_Texture);
        TEXTURE2D(_LensDirt_Texture);
        TEXTURE2D(_Grain_Texture);
        TEXTURE2D(_InternalLut);
        TEXTURE2D(_UserLut);
        TEXTURE2D(_BlueNoise_Texture);

        float4 _Lut_Params;
        float4 _UserLut_Params;
        float4 _Bloom_Params;
        float _Bloom_RGBM;
        float4 _LensDirt_Params;
        float _LensDirt_Intensity;
        float4 _Distortion_Params1;
        float4 _Distortion_Params2;
        float _Chroma_Params;
        half4 _Vignette_Params1;
        float4 _Vignette_Params2;
        float2 _Grain_Params;
        float4 _Grain_TilingParams;
        float4 _Bloom_Texture_TexelSize;
        float4 _Dithering_Params;

        #define DistCenter              _Distortion_Params1.xy
        #define DistAxis                _Distortion_Params1.zw
        #define DistTheta               _Distortion_Params2.x
        #define DistSigma               _Distortion_Params2.y
        #define DistScale               _Distortion_Params2.z
        #define DistIntensity           _Distortion_Params2.w

        #define ChromaAmount            _Chroma_Params.x

        #define BloomIntensity          _Bloom_Params.x
        #define BloomTint               _Bloom_Params.yzw
        #define BloomRGBM               _Bloom_RGBM.x
        #define LensDirtScale           _LensDirt_Params.xy
        #define LensDirtOffset          _LensDirt_Params.zw
        #define LensDirtIntensity       _LensDirt_Intensity.x

        #define VignetteColor           _Vignette_Params1.xyz
        #define VignetteCenter          _Vignette_Params2.xy
        #define VignetteIntensity       _Vignette_Params2.z
        #define VignetteSmoothness      _Vignette_Params2.w
        #define VignetteRoundness       _Vignette_Params1.w

        #define LutParams               _Lut_Params.xyz
        #define PostExposure            _Lut_Params.w
        #define UserLutParams           _UserLut_Params.xyz
        #define UserLutContribution     _UserLut_Params.w

        #define GrainIntensity          _Grain_Params.x
        #define GrainResponse           _Grain_Params.y
        #define GrainScale              _Grain_TilingParams.xy
        #define GrainOffset             _Grain_TilingParams.zw

        #define DitheringScale          _Dithering_Params.xy
        #define DitheringOffset         _Dithering_Params.zw

        float2 DistortUV(float2 uv)
        {
            // Note: this variant should never be set with XR
            #if _DISTORTION
            {
                uv = (uv - 0.5) * DistScale + 0.5;
                float2 ruv = DistAxis * (uv - 0.5 - DistCenter);
                float ru = length(float2(ruv));

                UNITY_BRANCH
                if (DistIntensity > 0.0)
                {
                    float wu = ru * DistTheta;
                    ru = tan(wu) * (rcp(ru * DistSigma));
                    uv = uv + ruv * (ru - 1.0);
                }
                else
                {
                    ru = rcp(ru) * DistTheta * atan(ru * DistSigma);
                    uv = uv + ruv * (ru - 1.0);
                }
            }
            #endif

            return uv;
        }

        half4 Frag(FullscreenVaryings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            float2 uvDistorted = DistortUV(uv);

            half3 color = (0.0).xxx;

            #if _CHROMATIC_ABERRATION
            {
                // Very fast version of chromatic aberration from HDRP using 3 samples and hardcoded
                // spectral lut. Performs significantly better on lower end GPUs.
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * ChromaAmount;
                float2 delta = (end - uv) / 3.0;

                half r = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uvDistorted                ).x;
                half g = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, DistortUV(delta + uv)      ).y;
                half b = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, DistortUV(delta * 2.0 + uv)).z;

                color = half3(r, g, b);
            }
            #else
            {
                color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uvDistorted).xyz;
            }
            #endif

            // Gamma space... Just do the rest of Uber in linear and convert back to sRGB at the end
            #if UNITY_COLORSPACE_GAMMA
            {
                color = SRGBToLinear(color);
            }
            #endif

            #if defined(BLOOM)
            {
                #if _BLOOM_HQ && !defined(SHADER_API_GLES)
                half4 bloom = SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_Bloom_Texture, sampler_LinearClamp), uvDistorted, _Bloom_Texture_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex);
                #else
                half4 bloom = SAMPLE_TEXTURE2D_X(_Bloom_Texture, sampler_LinearClamp, uvDistorted);
                #endif

                #if UNITY_COLORSPACE_GAMMA
                bloom.xyz *= bloom.xyz; // γ to linear
                #endif

                UNITY_BRANCH
                if (BloomRGBM > 0)
                {
                    bloom.xyz = DecodeRGBM(bloom);
                }

                bloom.xyz *= BloomIntensity;
                color += bloom.xyz * BloomTint;

                #if defined(BLOOM_DIRT)
                {
                    // UVs for the dirt texture should be DistortUV(uv * DirtScale + DirtOffset) but
                    // considering we use a cover-style scale on the dirt texture the difference
                    // isn't massive so we chose to save a few ALUs here instead in case lens
                    // distortion is active.
                    half3 dirt = SAMPLE_TEXTURE2D(_LensDirt_Texture, sampler_LinearClamp, uvDistorted * LensDirtScale + LensDirtOffset).xyz;
                    dirt *= LensDirtIntensity;
                    color += dirt * bloom.xyz;
                }
                #endif
            }
            #endif

            // To save on variants we'll use an uniform branch for vignette. Lower end platforms
            // don't like these but if we're running Uber it means we're running more expensive
            // effects anyway. Lower-end devices would limit themselves to on-tile compatible effect
            // and thus this shouldn't too much of a problem (famous last words).
            UNITY_BRANCH
            if (VignetteIntensity > 0)
            {
                color = ApplyVignette(color, uvDistorted, VignetteCenter, VignetteIntensity, VignetteRoundness, VignetteSmoothness, VignetteColor);
            }

            // Color grading is always enabled when post-processing/uber is active
            {
                color = ApplyColorGrading(color, PostExposure, TEXTURE2D_ARGS(_InternalLut, sampler_LinearClamp), LutParams, TEXTURE2D_ARGS(_UserLut, sampler_LinearClamp), UserLutParams, UserLutContribution);
            }

            #if _FILM_GRAIN
            {
                color = ApplyGrain(color, uv, TEXTURE2D_ARGS(_Grain_Texture, sampler_LinearRepeat), GrainIntensity, GrainResponse, GrainScale, GrainOffset);
            }
            #endif

            // Back to sRGB
            #if UNITY_COLORSPACE_GAMMA || _LINEAR_TO_SRGB_CONVERSION
            {
                color = LinearToSRGB(color);
            }
            #endif

            #if _DITHERING
            {
                color = ApplyDithering(color, uv, TEXTURE2D_ARGS(_BlueNoise_Texture, sampler_PointRepeat), DitheringScale, DitheringOffset);
            }
            #endif

            return half4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "UberPost"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
